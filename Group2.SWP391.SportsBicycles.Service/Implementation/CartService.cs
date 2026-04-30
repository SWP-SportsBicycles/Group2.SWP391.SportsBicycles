using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class CartService : ICartService
    {
        private readonly IGenericRepository<Cart> _cartRepo;
        private readonly IGenericRepository<CartItem> _cartItemRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<OrderItem> _orderItemRepo;
        private readonly IShippingProviderClient _shippingClient;
        private readonly IPaymentService _paymentService;
        private readonly IUnitOfWork _uow;

        public CartService(
            IGenericRepository<Cart> cartRepo,
            IGenericRepository<CartItem> cartItemRepo,
            IGenericRepository<Bike> bikeRepo,
            IGenericRepository<Order> orderRepo,
            IGenericRepository<OrderItem> orderItemRepo,
                IShippingProviderClient shippingClient,
                IPaymentService paymentService,

            IUnitOfWork uow)
        {
            _cartRepo = cartRepo;
            _cartItemRepo = cartItemRepo;
            _bikeRepo = bikeRepo;
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _shippingClient = shippingClient;
            _uow = uow;
                _paymentService = paymentService;
        }

        private static ResponseDTO Success(
            object? data = null,
            BusinessCode code = BusinessCode.GET_DATA_SUCCESSFULLY)
            => new()
            {
                IsSucess = true,
                BusinessCode = code,
                Data = data
            };

        private static ResponseDTO Fail(BusinessCode code, string msg)
            => new()
            {
                IsSucess = false,
                BusinessCode = code,
                Message = msg
            };


        private static string GetThumbnail(Bike bike)
        {
            return bike.Medias?
                .Where(m => !string.IsNullOrWhiteSpace(m.Image))
                .OrderBy(m => m.Type)
                .Select(m => m.Image!)
                .FirstOrDefault()
                ?? string.Empty;
        }

        private async Task ReleaseExpiredLockedOrdersAsync()
        {
            var now = DateTime.UtcNow;

            var expiredOrders = await _orderRepo.AsQueryable()
                .Include(o => o.Transaction)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                .Where(o =>
                    o.Status == OrderStatusEnum.Locked &&
                    o.ExpiresAt != null &&
                    o.ExpiresAt <= now)
                .ToListAsync();

            if (!expiredOrders.Any())
                return;

            foreach (var order in expiredOrders)
            {
                order.Status = OrderStatusEnum.Cancelled;

                if (order.Transaction != null &&
                    order.Transaction.Status == TransactionStatusEnum.Pending)
                {
                    order.Transaction.Status = TransactionStatusEnum.Failed;
                    order.Transaction.Description = "Order expired before payment";
                }

                foreach (var item in order.OrderItems)
                {
                    if (item.Bike != null &&
                        item.Bike.Status == BikeStatusEnum.Reserved)
                    {
                        item.Bike.Status = BikeStatusEnum.Available;
                    }
                }
            }

            await _uow.SaveChangeAsync();
        }

        private async Task<bool> HasActiveOrderForBikeAsync(Guid bikeId)
        {
            var now = DateTime.UtcNow;

            return await _orderRepo.AsQueryable()
                .AnyAsync(o =>
                    o.OrderItems.Any(oi => oi.BikeId == bikeId) &&
                    (
                        (
                            o.Status == OrderStatusEnum.Locked &&
                            o.ExpiresAt != null &&
                            o.ExpiresAt > now
                        )
                        ||
                        o.Status == OrderStatusEnum.Paid ||
                        o.Status == OrderStatusEnum.Confirmed ||
                        o.Status == OrderStatusEnum.Shipping
                    ));
        }

        public async Task<ResponseDTO> AddToCartAsync(Guid userId, AddToCartDTO dto)
        {
            if (userId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            await ReleaseExpiredLockedOrdersAsync();

            if (dto == null || dto.BikeId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "BikeId không hợp lệ");

            var bike = await _bikeRepo.AsQueryable()
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == dto.BikeId);

            if (bike == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy bike");

            if (bike.Listing == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy listing");

            if (bike.Listing.UserId == userId)
                return Fail(BusinessCode.INVALID_ACTION, "Không thể thêm xe của chính mình vào giỏ");

            if (bike.Listing.Status != ListingStatusEnum.Published)
                return Fail(BusinessCode.INVALID_ACTION, "Listing chưa được publish");

            if (bike.Status != BikeStatusEnum.Available)
                return Fail(BusinessCode.INVALID_ACTION, "Bike không khả dụng");

            var hasActiveOrder = await HasActiveOrderForBikeAsync(dto.BikeId);

            if (hasActiveOrder)
                return Fail(BusinessCode.INVALID_ACTION, "Bike đã có người đặt");

            var cart = await _cartRepo.AsQueryable()
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId
                };

                await _cartRepo.Insert(cart);
            }

            var existed = cart.CartItems.Any(x => x.BikeId == dto.BikeId);
            if (existed)
                return Fail(BusinessCode.INVALID_ACTION, "Bike đã có trong giỏ hàng");

            var cartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                BikeId = bike.Id,
                UnitPrice = bike.SalePrice,
                IsSelected = false
            };

            await _cartItemRepo.Insert(cartItem);
            await _uow.SaveChangeAsync();

            return Success(new
            {
                CartId = cart.Id,
                CartItemId = cartItem.Id,
                BikeId = cartItem.BikeId,
                UnitPrice = cartItem.UnitPrice,
                IsSelected = cartItem.IsSelected
            }, BusinessCode.CREATED_SUCCESSFULLY);
        }

        public async Task<ResponseDTO> UpdateCartItemSelectionAsync(Guid userId, UpdateCartItemSelectionDTO dto)
        {
            if (userId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (dto == null || dto.CartItemId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "CartItemId không hợp lệ");

            var cartItem = await _cartItemRepo.AsQueryable()
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == dto.CartItemId && ci.Cart.UserId == userId);

            if (cartItem == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy cart item");

            cartItem.IsSelected = dto.IsSelected;

            await _cartItemRepo.Update(cartItem);
            await _uow.SaveChangeAsync();

            return Success(new
            {
                CartItemId = cartItem.Id,
                cartItem.IsSelected
            }, BusinessCode.UPDATE_SUCESSFULLY);
        }

        public async Task<ResponseDTO> GetMyCartAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            await ReleaseExpiredLockedOrdersAsync();

            var cart = await _cartRepo.AsQueryable()
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Bike)
                        .ThenInclude(b => b.Listing)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Bike)
                        .ThenInclude(b => b.Medias)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return Success(new
                {
                    CartId = Guid.Empty,
                    TotalItems = 0,
                    SelectedCount = 0,
                    SubTotal = 0m,
                    Items = new List<object>()
                });
            }

            var items = cart.CartItems.Select(ci => new
            {
                CartItemId = ci.Id,
                BikeId = ci.BikeId,
                UnitPrice = ci.UnitPrice,
                IsSelected = ci.IsSelected,
                Bike = ci.Bike == null ? null : new
                {
                    ci.Bike.Brand,
                    ci.Bike.Category,
                    ci.Bike.FrameSize,
                    ci.Bike.SalePrice,
                    Title = ci.Bike.Listing?.Title,
                    Thumbnail = GetThumbnail(ci.Bike)
                }
            }).ToList();

            return Success(new
            {
                CartId = cart.Id,
                TotalItems = cart.CartItems.Count,
                SelectedCount = cart.CartItems.Count(x => x.IsSelected),
                SubTotal = cart.CartItems
                    .Where(x => x.IsSelected)
                    .Sum(x => x.UnitPrice),
                Items = items
            });
        }

        public async Task<ResponseDTO> RemoveCartItemAsync(Guid userId, Guid cartItemId)
        {
            if (userId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (cartItemId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "CartItemId không hợp lệ");

            var item = await _cartItemRepo.AsQueryable()
                .Include(x => x.Cart)
                .FirstOrDefaultAsync(x => x.Id == cartItemId && x.Cart.UserId == userId);

            if (item == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy cart item");

            await _cartItemRepo.Delete(item);
            await _uow.SaveChangeAsync();

            return Success(null, BusinessCode.DELETE_SUCESSFULLY);
        }

        public async Task<ResponseDTO> CreateOrderFromSelectedCartAsync(Guid buyerId, CreateOrderFromCartDTO dto)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định người dùng");

            if (dto == null)
                return Fail(BusinessCode.INVALID_INPUT, "Dữ liệu không hợp lệ");

            var cart = await _cartRepo.AsQueryable()
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Bike)
                        .ThenInclude(b => b.Listing)
                            .ThenInclude(l => l.User)
                                .ThenInclude(u => u.SellerShippingProfiles)
                .FirstOrDefaultAsync(c => c.UserId == buyerId);

            if (cart == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không có giỏ hàng");

            var items = cart.CartItems.Where(x => x.IsSelected).ToList();
            if (!items.Any())
                return Fail(BusinessCode.INVALID_ACTION, "Chưa chọn sản phẩm");

            foreach (var item in items)
            {
                var bike = item.Bike!;
                if (bike.Listing!.Status != ListingStatusEnum.Published || bike.Status != BikeStatusEnum.Available)
                    return Fail(BusinessCode.INVALID_ACTION, "Sản phẩm không khả dụng");

                var hasActive = await _orderRepo.AsQueryable()
                    .AnyAsync(o =>
                        o.OrderItems.Any(oi => oi.BikeId == bike.Id) &&
                        (
                            o.Status == OrderStatusEnum.Paid ||
                            o.Status == OrderStatusEnum.Confirmed ||
                            o.Status == OrderStatusEnum.Shipping
                        ));

                if (hasActive)
                    return Fail(BusinessCode.INVALID_ACTION, "Có sản phẩm đã được mua");
            }

            var sellerProfile = items.First().Bike!.Listing!.User!.SellerShippingProfiles!
                .OrderByDescending(x => x.IsDefault)
                .FirstOrDefault();

            var subTotal = items.Sum(x => x.Bike!.SalePrice > 0 ? x.Bike.SalePrice : x.Bike.Price);
            int weight = items
    .Where(x => x.Bike != null)
    .Sum(x => (int)(x.Bike!.Weight * 1000));

            var fee = await _shippingClient.CalculateFeeAsync(
      "GHN",
      sellerProfile!.FromDistrictId,
      sellerProfile.FromWardCode,
      dto.ToDistrictId,
      dto.ToWardCode,
      (int)subTotal,
      weight // 👈 FIX
  );

            var shippingFee = fee.Fee;
            var total = subTotal + shippingFee;

            await _uow.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = buyerId,
                    Status = OrderStatusEnum.Pending,
                    ReceiverName = dto.ReceiverName.Trim(),
                    ReceiverPhone = dto.ReceiverPhone.Trim(),
                    ReceiverAddress = dto.ReceiverAddress.Trim(),
                    ToDistrictId = dto.ToDistrictId,
                    ToWardCode = dto.ToWardCode,// 🔥 không lock
                    ExpiresAt = null,
                    SubTotal = subTotal,
                    ShippingFee = shippingFee,
                    TotalAmount = total
                };

                await _orderRepo.Insert(order);

                foreach (var item in items)
                {
                    await _orderItemRepo.Insert(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        BikeId = item.BikeId,
                        UnitPrice = item.Bike!.SalePrice > 0 ? item.Bike.SalePrice : item.Bike.Price,
                        LineTotal = item.Bike!.SalePrice > 0 ? item.Bike.SalePrice : item.Bike.Price
                    });

                    await _cartItemRepo.Delete(item);
                }

                await _uow.SaveChangeAsync();
                await _uow.CommitAsync();

                var payment = await _paymentService.CreatePaymentLink(buyerId, order.Id);
                if (!payment.IsSucess) return payment;

                return Success(new
                {
                    OrderId = order.Id,
                    TotalAmount = total,
                    Status = "Pending",
                    Payment = payment.Data
                });
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
        public async Task<ResponseDTO> PreviewCheckoutAsync(Guid userId, PreviewCheckoutDTO dto)
        {
            if (userId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            await ReleaseExpiredLockedOrdersAsync();

            if (dto == null)
                return Fail(BusinessCode.INVALID_INPUT, "Dữ liệu không hợp lệ");

            if (string.IsNullOrWhiteSpace(dto.ReceiverName))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu tên người nhận");

            if (string.IsNullOrWhiteSpace(dto.ReceiverPhone))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu số điện thoại người nhận");

            if (string.IsNullOrWhiteSpace(dto.ReceiverAddress))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu địa chỉ người nhận");

            if (dto.ToDistrictId <= 0 || string.IsNullOrWhiteSpace(dto.ToWardCode))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu thông tin địa chỉ giao hàng");

            var cart = await _cartRepo.AsQueryable()
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Bike)
                        .ThenInclude(b => b.Listing)
                            .ThenInclude(l => l.User)
                                .ThenInclude(u => u.SellerShippingProfiles)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Bike)
                        .ThenInclude(b => b.Medias)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy giỏ hàng");

            var selectedItems = cart.CartItems
                .Where(x => x.IsSelected)
                .ToList();

            if (!selectedItems.Any())
                return Fail(BusinessCode.INVALID_ACTION, "Vui lòng chọn ít nhất 1 sản phẩm");

            foreach (var item in selectedItems)
            {
                if (item.Bike == null)
                    return Fail(BusinessCode.DATA_NOT_FOUND, "Bike không tồn tại");

                if (item.Bike.Listing == null)
                    return Fail(BusinessCode.DATA_NOT_FOUND, "Listing không tồn tại");

                if (item.Bike.Listing.Status != ListingStatusEnum.Published)
                    return Fail(BusinessCode.INVALID_ACTION, "Có sản phẩm chưa được publish");

                if (item.Bike.Status != BikeStatusEnum.Available)
                    return Fail(BusinessCode.INVALID_ACTION, $"Bike {item.Bike.Id} không còn khả dụng");

                var hasActiveOrder = await HasActiveOrderForBikeAsync(item.BikeId);

                if (hasActiveOrder)
                    return Fail(BusinessCode.INVALID_ACTION, $"Bike {item.BikeId} đã có người đặt");
            }

            var sellerIds = selectedItems
                .Select(x => x.Bike!.Listing!.UserId)
                .Distinct()
                .ToList();

            if (sellerIds.Count > 1)
                return Fail(BusinessCode.INVALID_ACTION, "Chỉ được thanh toán các sản phẩm của cùng một seller trong một đơn hàng");

            var sellerProfile = selectedItems
                .Select(x => x.Bike!.Listing!.User?.SellerShippingProfiles?
                    .OrderByDescending(p => p.IsDefault)
                    .ThenByDescending(p => p.CreatedAt)
                    .FirstOrDefault())
                .FirstOrDefault();

            if (sellerProfile == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Seller chưa cấu hình địa chỉ giao hàng");

            foreach (var item in selectedItems)
            {
                item.UnitPrice = item.Bike!.SalePrice;
            }

            var subTotal = selectedItems.Sum(x => x.UnitPrice);


            int weight = selectedItems
    .Where(x => x.Bike != null)
    .Sum(x => (int)(x.Bike!.Weight * 1000));

            var feeResult = await _shippingClient.CalculateFeeAsync(
    "GHN",
    sellerProfile.FromDistrictId,
    sellerProfile.FromWardCode,
    dto.ToDistrictId,
    dto.ToWardCode,
    (int)subTotal,
    weight // 👈 FIX
);

            if (!feeResult.IsSuccess)
                return Fail(BusinessCode.INVALID_ACTION, feeResult.ErrorMessage ?? "Không tính được phí ship GHN");

            var shippingFee = feeResult.Fee;

            var totalAmount = subTotal + shippingFee;

            return Success(new
            {
                ReceiverName = dto.ReceiverName.Trim(),
                ReceiverPhone = dto.ReceiverPhone.Trim(),
                ReceiverAddress = dto.ReceiverAddress.Trim(),
                SubTotal = subTotal,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                Items = selectedItems.Select(x => new
                {
                    BikeId = x.BikeId,
                    UnitPrice = x.UnitPrice,
                    Thumbnail = GetThumbnail(x.Bike!)
                }).ToList()
            });
        }
    }
}