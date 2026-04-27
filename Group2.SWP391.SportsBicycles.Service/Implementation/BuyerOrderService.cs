using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class BuyerOrderService : IBuyerOrderService
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<OrderItem> _orderItemRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IPaymentService _paymentService;
        private readonly IShippingProviderClient _shippingProviderClient;
        private readonly IUnitOfWork _uow;


        public BuyerOrderService(
       IGenericRepository<Order> orderRepo,
       IGenericRepository<OrderItem> orderItemRepo,
       IGenericRepository<Bike> bikeRepo,
       IShippingProviderClient shippingProviderClient,
       IUnitOfWork uow,
       IPaymentService paymentService)
        {
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _bikeRepo = bikeRepo;
            _shippingProviderClient = shippingProviderClient;
            _uow = uow;
            _paymentService = paymentService;
        }

        // ================= HELPER =================

        private static ResponseDTO Success(object? data = null, BusinessCode code = BusinessCode.GET_DATA_SUCCESSFULLY)
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

        // ================= CREATE ORDER =================
        public async Task<ResponseDTO> CreateOrderAsync(Guid buyerId, CreateOrderDTO dto)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (dto == null || dto.BikeId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "Dữ liệu không hợp lệ");

            if (string.IsNullOrWhiteSpace(dto.ReceiverName) ||
                string.IsNullOrWhiteSpace(dto.ReceiverPhone) ||
                string.IsNullOrWhiteSpace(dto.ReceiverAddress))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu thông tin người nhận");

            if (dto.ToDistrictId <= 0 || string.IsNullOrWhiteSpace(dto.ToWardCode))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu thông tin địa chỉ giao hàng");

            await _uow.BeginTransactionAsync();

            Guid orderId;
            decimal subTotal, shippingFee, totalAmount;
            object itemData;

            try
            {
                var bike = await _bikeRepo.AsQueryable()
                    .Include(b => b.Listing)
                        .ThenInclude(l => l.User)
                            .ThenInclude(u => u.SellerShippingProfiles)
                    .FirstOrDefaultAsync(b => b.Id == dto.BikeId);

                if (bike == null || bike.Listing == null)
                    return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy sản phẩm");

                if (bike.Listing.UserId == buyerId)
                    return Fail(BusinessCode.INVALID_ACTION, "Không thể mua xe của chính mình");

                if (bike.Listing.Status != ListingStatusEnum.Published)
                    return Fail(BusinessCode.INVALID_ACTION, "Listing chưa được publish");

                if (bike.Status != BikeStatusEnum.Available)
                    return Fail(BusinessCode.INVALID_ACTION, "Bike không khả dụng");

                // 🔥 chỉ block khi đã PAID
                var hasActiveOrder = await _orderRepo.AsQueryable()
                    .AnyAsync(o =>
                        o.OrderItems.Any(oi => oi.BikeId == dto.BikeId) &&
                        (
                            o.Status == OrderStatusEnum.Paid ||
                            o.Status == OrderStatusEnum.Confirmed ||
                            o.Status == OrderStatusEnum.Shipping
                        ));

                if (hasActiveOrder)
                    return Fail(BusinessCode.INVALID_ACTION, "Bike đã được mua");

                var sellerProfile = bike.Listing.User?.SellerShippingProfiles?
                    .OrderByDescending(x => x.IsDefault)
                    .ThenByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

                if (sellerProfile == null)
                    return Fail(BusinessCode.DATA_NOT_FOUND, "Seller chưa cấu hình giao hàng");

                var price = bike.SalePrice > 0 ? bike.SalePrice : bike.Price;

                subTotal = price;

                var fee = await _shippingProviderClient.CalculateFeeAsync(
                    "GHN",
                    sellerProfile.FromDistrictId,
                    sellerProfile.FromWardCode,
                    dto.ToDistrictId,
                    dto.ToWardCode,
                    (int)price
                );

                if (!fee.IsSuccess)
                    return Fail(BusinessCode.INVALID_ACTION, "Không tính được phí ship");

                shippingFee = fee.Fee;
                totalAmount = subTotal + shippingFee;

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = buyerId,
                    Status = OrderStatusEnum.Pending, // 🔥 không lock
                    ExpiresAt = null,

                    ReceiverName = dto.ReceiverName.Trim(),
                    ReceiverPhone = dto.ReceiverPhone.Trim(),
                    ReceiverAddress = dto.ReceiverAddress.Trim(),
                    ToProvinceId = dto.ToProvinceId,
                    ToProvinceName = dto.ToProvinceName?.Trim(),
                    ToDistrictName = dto.ToDistrictName?.Trim(),
                    ToWardName = dto.ToWardName?.Trim(),

                    ToDistrictId = dto.ToDistrictId,
                    ToWardCode = dto.ToWardCode.Trim(),

                    SubTotal = subTotal,
                    ShippingFee = shippingFee,
                    TotalAmount = totalAmount
                };

                await _orderRepo.Insert(order);

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    BikeId = bike.Id,
                    UnitPrice = price,
                    LineTotal = price
                };

                await _orderItemRepo.Insert(orderItem);

                await _uow.SaveChangeAsync();
                await _uow.CommitAsync();

                orderId = order.Id;

                itemData = new
                {
                    BikeId = bike.Id,
                    UnitPrice = price
                };
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            var payment = await _paymentService.CreatePaymentLink(buyerId, orderId);
            if (!payment.IsSucess) return payment;

            return Success(new
            {
                OrderId = orderId,
                SubTotal = subTotal,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                Status = "Pending",
                Payment = payment.Data,
                Items = new List<object> { itemData }
            });
        }
        public async Task<ResponseDTO> MarkOrderPaidAsync(Guid orderId)
        {
            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            if (order.Status != OrderStatusEnum.Pending)
                return Fail(BusinessCode.INVALID_ACTION, "Chỉ order Pending mới được chuyển Paid");

            order.Status = OrderStatusEnum.Paid;

            var bike = order.OrderItems.FirstOrDefault()?.Bike;
            if (bike != null)
            {
                bike.Status = BikeStatusEnum.Reserved;
            }

            await _uow.SaveChangeAsync();

            return Success(new
            {
                orderId = order.Id,
                status = order.Status.ToString()
            }, BusinessCode.UPDATE_SUCESSFULLY);
        }



        // ================= GET MY ORDERS =================
        // ================= GET MY ORDERS =================
        public async Task<ResponseDTO> GetMyOrdersAsync(Guid buyerId, int pageNumber, int pageSize)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "BuyerId không hợp lệ");

            if (pageNumber <= 0 || pageSize <= 0)
                return Fail(BusinessCode.INVALID_INPUT, "Invalid pagination");

            var query = _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                        .ThenInclude(b => b.Listing)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                        .ThenInclude(b => b.Medias)
                .Include(o => o.Transaction)
                .Include(o => o.Shipment)
                .Where(o => o.UserId == buyerId)
                .OrderByDescending(o => o.CreatedAt);

            var totalItems = await query.CountAsync();

            var orders = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = orders.Select(o => new
            {
                OrderId = o.Id,
                Status = o.Status.ToString(),
                TotalAmount = o.TotalAmount,
                SubTotal = o.SubTotal,
                ShippingFee = o.ShippingFee,

                ReceiverName = o.ReceiverName,
                ReceiverPhone = o.ReceiverPhone,
                ReceiverAddress = o.ReceiverAddress,
                ToDistrictId = o.ToDistrictId,
                ToWardCode = o.ToWardCode,

                ToProvinceId = o.ToProvinceId,
                ToProvinceName = o.ToProvinceName,
                ToDistrictName = o.ToDistrictName,
                ToWardName = o.ToWardName,

                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                ExpiresAt = o.ExpiresAt,
                CompletedAt = o.CompletedAt,

                Payment = o.Transaction == null ? null : new
                {
                    TransactionId = o.Transaction.Id,
                    TransactionStatus = o.Transaction.Status.ToString(),
                    PaymentLink = o.Transaction.PaymentLink,
                    ProviderOrderCode = o.Transaction.ProviderOrderCode,
                    PaidAt = o.Transaction.PaidAt
                },

                Shipment = o.Shipment == null ? null : new
                {
                    ShipmentId = o.Shipment.Id,
                    ShipmentCode = o.Shipment.ShipmentCode,
                    ProviderOrderCode = o.Shipment.ProviderOrderCode,
                    ShippingProvider = o.Shipment.ShippingProvider,
                    ShipmentStatus = o.Shipment.Status.ToString(),
                    ShippingFee = o.Shipment.ShippingFee,
                    DeliveredAt = o.Shipment.DeliveredAt
                },

                Items = o.OrderItems.Select(oi => new
                {
                    OrderItemId = oi.Id,
                    BikeId = oi.BikeId,
                    UnitPrice = oi.UnitPrice,
                    LineTotal = oi.LineTotal,

                    Bike = oi.Bike == null ? null : new
                    {
                        Id = oi.Bike.Id,
                        ListingId = oi.Bike.ListingId,

                        SerialNumber = oi.Bike.SerialNumber,
                        Category = oi.Bike.Category,
                        Brand = oi.Bike.Brand,
                        FrameSize = oi.Bike.FrameSize,
                        FrameMaterial = oi.Bike.FrameMaterial,
                        Condition = oi.Bike.Condition,

                        Paint = oi.Bike.Paint,
                        Groupset = oi.Bike.Groupset,
                        Operating = oi.Bike.Operating,
                        TireRim = oi.Bike.TireRim,
                        BrakeType = oi.Bike.BrakeType,

                        Overall = oi.Bike.Overall,

                        Price = oi.Bike.Price,
                        OriginalPrice = oi.Bike.OriginalPrice,
                        SalePrice = oi.Bike.SalePrice,

                        Status = oi.Bike.Status.ToString(),
                        City = oi.Bike.City,

                        Title = oi.Bike.Listing == null ? null : oi.Bike.Listing.Title,
                        Description = oi.Bike.Listing == null ? null : oi.Bike.Listing.Description,

                        Images = oi.Bike.Medias
                            .Where(m => !string.IsNullOrWhiteSpace(m.Image))
                            .Select(m => m.Image)
                            .ToList(),

                        VideoUrls = oi.Bike.Medias
                            .Where(m => !string.IsNullOrWhiteSpace(m.VideoUrl))
                            .Select(m => m.VideoUrl)
                            .ToList(),

                        Thumbnail = oi.Bike.Medias
                            .Where(m => m.Type == MediaType.Thumbnail && !string.IsNullOrWhiteSpace(m.Image))
                            .Select(m => m.Image)
                            .FirstOrDefault()
                            ?? oi.Bike.Medias
                                .Where(m => !string.IsNullOrWhiteSpace(m.Image))
                                .Select(m => m.Image)
                                .FirstOrDefault()
                            ?? string.Empty,

                        Medias = oi.Bike.Medias.Select(m => new
                        {
                            MediaId = m.Id,
                            Image = m.Image,
                            VideoUrl = m.VideoUrl,
                            Type = m.Type.ToString()
                        }).ToList()
                    }
                }).ToList()
            }).ToList();

            return Success(new
            {
                Items = items,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        // ================= GET ORDER DETAIL =================
        public async Task<ResponseDTO> GetOrderDetailAsync(Guid buyerId, Guid orderId)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "BuyerId không hợp lệ");

            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                        .ThenInclude(b => b.Listing)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                        .ThenInclude(b => b.Medias)
                .Include(o => o.Transaction)
                .Include(o => o.Shipment)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == buyerId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            var data = new
            {
                OrderId = order.Id,
                Status = order.Status.ToString(),
                TotalAmount = order.TotalAmount,
                SubTotal = order.SubTotal,
                ShippingFee = order.ShippingFee,

                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,

                // ===== FULL ADDRESS (QUAN TRỌNG) =====
                Address = new
                {
                    ProvinceId = order.ToProvinceId,
                    ProvinceName = order.ToProvinceName,

                    DistrictId = order.ToDistrictId,
                    DistrictName = order.ToDistrictName,

                    WardCode = order.ToWardCode,
                    WardName = order.ToWardName,

                    Detail = order.ReceiverAddress,

                    FullAddress = string.Join(", ", new[]
          {
            order.ReceiverAddress,
            order.ToWardName,
            order.ToDistrictName,
            order.ToProvinceName
        }.Where(x => !string.IsNullOrWhiteSpace(x)))
                },

                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                ExpiresAt = order.ExpiresAt,
                CompletedAt = order.CompletedAt,

                Payment = order.Transaction == null ? null : new
                {
                    TransactionId = order.Transaction.Id,
                    OrderCode = order.Transaction.OrderCode,
                    ProviderOrderCode = order.Transaction.ProviderOrderCode,
                    PaymentLink = order.Transaction.PaymentLink,
                    TransactionStatus = order.Transaction.Status.ToString(),
                    Amount = order.Transaction.Amount,
                    PaidAt = order.Transaction.PaidAt,
                    Description = order.Transaction.Description
                },

                Shipment = order.Shipment == null ? null : new
                {
                    ShipmentId = order.Shipment.Id,
                    ShipmentCode = order.Shipment.ShipmentCode,
                    ProviderOrderCode = order.Shipment.ProviderOrderCode,
                    ShippingProvider = order.Shipment.ShippingProvider,
                    ShipmentStatus = order.Shipment.Status.ToString(),
                    ShippingFee = order.Shipment.ShippingFee
                },

                Items = order.OrderItems.Select(oi => new
                {
                    OrderItemId = oi.Id,
                    BikeId = oi.BikeId,
                    UnitPrice = oi.UnitPrice,
                    LineTotal = oi.LineTotal,

                    Bike = oi.Bike == null ? null : new
                    {
                        Id = oi.Bike.Id,
                        ListingId = oi.Bike.ListingId,

                        SerialNumber = oi.Bike.SerialNumber,
                        Category = oi.Bike.Category,
                        Brand = oi.Bike.Brand,
                        FrameSize = oi.Bike.FrameSize,
                        FrameMaterial = oi.Bike.FrameMaterial,
                        Condition = oi.Bike.Condition,

                        Paint = oi.Bike.Paint,
                        Groupset = oi.Bike.Groupset,
                        Operating = oi.Bike.Operating,
                        TireRim = oi.Bike.TireRim,
                        BrakeType = oi.Bike.BrakeType,

                        Overall = oi.Bike.Overall,

                        Price = oi.Bike.Price,
                        OriginalPrice = oi.Bike.OriginalPrice,
                        SalePrice = oi.Bike.SalePrice,

                        Status = oi.Bike.Status.ToString(),

                        // 👉 FIX luôn city cho chuẩn (nếu cần mapping)
                        City = oi.Bike.City,
                        Title = oi.Bike.Listing?.Title,
                        Description = oi.Bike.Listing?.Description,

                        Images = oi.Bike.Medias
                            .Where(m => !string.IsNullOrWhiteSpace(m.Image))
                            .Select(m => m.Image)
                            .ToList(),

                        VideoUrls = oi.Bike.Medias
                            .Where(m => !string.IsNullOrWhiteSpace(m.VideoUrl))
                            .Select(m => m.VideoUrl)
                            .ToList(),

                        Thumbnail = oi.Bike.Medias
                            .Where(m => m.Type == MediaType.Thumbnail && !string.IsNullOrWhiteSpace(m.Image))
                            .Select(m => m.Image)
                            .FirstOrDefault()
                            ?? oi.Bike.Medias
                                .Where(m => !string.IsNullOrWhiteSpace(m.Image))
                                .Select(m => m.Image)
                                .FirstOrDefault()
                            ?? string.Empty,

                        Medias = oi.Bike.Medias.Select(m => new
                        {
                            MediaId = m.Id,
                            Image = m.Image,
                            VideoUrl = m.VideoUrl,
                            Type = m.Type.ToString()
                        }).ToList()
                    }
                }).ToList()
            };

            return Success(data);
        }

        public async Task<ResponseDTO> CancelOrderAsync(Guid buyerId, Guid orderId)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "BuyerId không hợp lệ");

            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == buyerId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            // 🔥 chỉ cho cancel khi Locked hoặc Pending
            if (order.Status != OrderStatusEnum.Locked &&
                order.Status != OrderStatusEnum.Pending)
            {
                return Fail(BusinessCode.INVALID_ACTION,
                    "Chỉ order Locked hoặc Pending mới được hủy");
            }

            // update status
            order.Status = OrderStatusEnum.Cancelled;

            // 🔥 release bike
            foreach (var item in order.OrderItems)
            {
                if (item.Bike != null)
                {
                    item.Bike.Status = BikeStatusEnum.Available;
                }
            }

            await _uow.SaveChangeAsync();

            return Success(new
            {
                orderId = order.Id,
                status = order.Status.ToString()
            }, BusinessCode.UPDATE_SUCESSFULLY);
        }
    }
}