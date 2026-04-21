using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Group2.SWP391.SportsBicycles.Services.Helpers;
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
        private readonly IUnitOfWork _uow;

        public CartService(
            IGenericRepository<Cart> cartRepo,
            IGenericRepository<CartItem> cartItemRepo,
            IGenericRepository<Bike> bikeRepo,
            IGenericRepository<Order> orderRepo,
            IGenericRepository<OrderItem> orderItemRepo,
            IUnitOfWork uow)
        {
            _cartRepo = cartRepo;
            _cartItemRepo = cartItemRepo;
            _bikeRepo = bikeRepo;
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _uow = uow;
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

        public async Task<ResponseDTO> AddToCartAsync(Guid userId, AddToCartDTO dto)
        {
            if (userId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

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

            var hasActiveOrder = await _orderRepo.AsQueryable()
                .AnyAsync(o =>
                    (o.Status == OrderStatusEnum.Locked ||
                     o.Status == OrderStatusEnum.Pending ||
                     o.Status == OrderStatusEnum.Paid ||
                     o.Status == OrderStatusEnum.Confirmed ||
                     o.Status == OrderStatusEnum.Shipping)
                    && o.OrderItems.Any(oi => oi.BikeId == dto.BikeId));

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

            var cart = await _cartRepo.AsQueryable()
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Bike)
                        .ThenInclude(b => b.Listing)
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
                    Title = ci.Bike.Listing?.Title
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
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

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

            await _uow.BeginTransactionAsync();

            try
            {
                var cart = await _cartRepo.AsQueryable()
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Bike)
                            .ThenInclude(b => b.Listing)
                    .FirstOrDefaultAsync(c => c.UserId == buyerId);

                if (cart == null)
                    return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy giỏ hàng");

                var selectedItems = cart.CartItems
                    .Where(x => x.IsSelected)
                    .ToList();

                if (!selectedItems.Any())
                    return Fail(BusinessCode.INVALID_ACTION, "Vui lòng chọn ít nhất 1 sản phẩm để thanh toán");

                foreach (var item in selectedItems)
                {
                    if (item.Bike == null)
                        return Fail(BusinessCode.DATA_NOT_FOUND, "Bike không tồn tại");

                    if (item.Bike.Listing == null)
                        return Fail(BusinessCode.DATA_NOT_FOUND, "Listing không tồn tại");

                    if (item.Bike.Listing.UserId == buyerId)
                        return Fail(BusinessCode.INVALID_ACTION, "Không thể mua xe của chính mình");

                    if (item.Bike.Listing.Status != ListingStatusEnum.Published)
                        return Fail(BusinessCode.INVALID_ACTION, "Có sản phẩm chưa được publish");

                    if (item.Bike.Status != BikeStatusEnum.Available)
                        return Fail(BusinessCode.INVALID_ACTION, $"Bike {item.Bike.Id} không còn khả dụng");

                    var hasActiveOrder = await _orderRepo.AsQueryable()
                        .AnyAsync(o =>
                            (o.Status == OrderStatusEnum.Locked ||
                             o.Status == OrderStatusEnum.Pending ||
                             o.Status == OrderStatusEnum.Paid ||
                             o.Status == OrderStatusEnum.Confirmed ||
                             o.Status == OrderStatusEnum.Shipping)
                            && o.OrderItems.Any(oi => oi.BikeId == item.BikeId));

                    if (hasActiveOrder)
                        return Fail(BusinessCode.INVALID_ACTION, $"Bike {item.BikeId} đã có người đặt");
                }


                foreach (var item in selectedItems)
                {
                    if (item.Bike == null)
                        return Fail(BusinessCode.DATA_NOT_FOUND, "Bike không tồn tại");

                    item.UnitPrice = item.Bike.SalePrice;
                }
                var subTotal = selectedItems.Sum(x => x.UnitPrice);

                var shippingFee = dto.DistanceKm > 0
                    ? ShippingFeeCalculator.Calculate((decimal)dto.DistanceKm)
                    : 30000;

                var totalAmount = subTotal + shippingFee;

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = buyerId,
                    Status = OrderStatusEnum.Locked,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    ReceiverName = dto.ReceiverName.Trim(),
                    ReceiverPhone = dto.ReceiverPhone.Trim(),
                    ReceiverAddress = dto.ReceiverAddress.Trim(),
                    ToDistrictId = dto.ToDistrictId,
                    ToWardCode = dto.ToWardCode,
                    SubTotal = subTotal,
                    ShippingFee = shippingFee,
                    TotalAmount = totalAmount
                };

                await _orderRepo.Insert(order);

                foreach (var item in selectedItems)
                {
                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        BikeId = item.BikeId,
                        UnitPrice = item.UnitPrice,
                        LineTotal = item.UnitPrice
                    };

                    await _orderItemRepo.Insert(orderItem);

                    item.Bike!.Status = BikeStatusEnum.Reserved;
                }

                foreach (var item in selectedItems)
                {
                    await _cartItemRepo.Delete(item);
                }

                await _uow.SaveChangeAsync();
                await _uow.CommitAsync();

                return Success(new
                {
                    OrderId = order.Id,
                    SubTotal = order.SubTotal,
                    ShippingFee = order.ShippingFee,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status.ToString(),
                    ExpiresAt = order.ExpiresAt
                }, BusinessCode.CREATED_SUCCESSFULLY);
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

                var hasActiveOrder = await _orderRepo.AsQueryable()
                    .AnyAsync(o =>
                        (o.Status == OrderStatusEnum.Locked ||
                         o.Status == OrderStatusEnum.Pending ||
                         o.Status == OrderStatusEnum.Paid ||
                         o.Status == OrderStatusEnum.Confirmed ||
                         o.Status == OrderStatusEnum.Shipping)
                        && o.OrderItems.Any(oi => oi.BikeId == item.BikeId));

                if (hasActiveOrder)
                    return Fail(BusinessCode.INVALID_ACTION, $"Bike {item.BikeId} đã có người đặt");
            }

            foreach (var item in selectedItems)
            {
                if (item.Bike == null)
                    return Fail(BusinessCode.DATA_NOT_FOUND, "Bike không tồn tại");

                item.UnitPrice = item.Bike.SalePrice;
            }
            var subTotal = selectedItems.Sum(x => x.UnitPrice);
            var shippingFee = ShippingFeeCalculator.Calculate(dto.DistanceKm);
            var totalAmount = subTotal + shippingFee;

            return Success(new
            {
                ReceiverName = dto.ReceiverName.Trim(),
                ReceiverPhone = dto.ReceiverPhone.Trim(),
                ReceiverAddress = dto.ReceiverAddress.Trim(),
                SubTotal = subTotal,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount
            });
        }
    }
}