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
    public class ShipmentService : IShipmentService
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Shipment> _shipmentRepo;
        private readonly IGenericRepository<ShipmentTracking> _trackingRepo;
        private readonly IUnitOfWork _uow;
        private readonly IShippingProviderClient _shippingProviderClient;
        private readonly IGenericRepository<SellerShippingProfile> _sellerProfileRepo;
        public ShipmentService(
     IGenericRepository<Order> orderRepo,
     IGenericRepository<Shipment> shipmentRepo,
     IGenericRepository<ShipmentTracking> trackingRepo,
     IGenericRepository<SellerShippingProfile> sellerProfileRepo,
     IUnitOfWork uow,
     IShippingProviderClient shippingProviderClient)
        {
            _orderRepo = orderRepo;
            _shipmentRepo = shipmentRepo;
            _trackingRepo = trackingRepo;
            _sellerProfileRepo = sellerProfileRepo;
            _uow = uow;
            _shippingProviderClient = shippingProviderClient;
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

        private static string GenerateShipmentCode()
        {
            return $"SP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        }

        private static string GetTrackingTitle(ShipmentStatusEnum status)
        {
            return status switch
            {
                ShipmentStatusEnum.Created => "Đã tạo vận đơn",
                ShipmentStatusEnum.PickingUp => "Đang lấy hàng",
                ShipmentStatusEnum.PickedUp => "Đã lấy hàng",
                ShipmentStatusEnum.InTransit => "Đang vận chuyển",
                ShipmentStatusEnum.Delivered => "Đã giao hàng",
                ShipmentStatusEnum.Failed => "Giao hàng thất bại",
                ShipmentStatusEnum.Cancelled => "Đã huỷ vận đơn",
                _ => "Cập nhật vận đơn"
            };
        }

        private static ShipmentStatusEnum MapRawStatus(string? rawStatus)
        {
            if (string.IsNullOrWhiteSpace(rawStatus))
                return ShipmentStatusEnum.Pending;

            rawStatus = rawStatus.Trim().ToLower();

            return rawStatus switch
            {
                "created" => ShipmentStatusEnum.Created,
                "picking_up" => ShipmentStatusEnum.PickingUp,
                "picked_up" => ShipmentStatusEnum.PickedUp,
                "in_transit" => ShipmentStatusEnum.InTransit,
                "delivered" => ShipmentStatusEnum.Delivered,
                "failed" => ShipmentStatusEnum.Failed,
                "cancelled" => ShipmentStatusEnum.Cancelled,
                _ => ShipmentStatusEnum.Pending
            };
        }

        private static string BuildLocation(
            string? wardName,
            string? districtName,
            string? provinceName,
            string? streetAddress)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(streetAddress))
                parts.Add(streetAddress.Trim());

            if (!string.IsNullOrWhiteSpace(wardName))
                parts.Add(wardName.Trim());

            if (!string.IsNullOrWhiteSpace(districtName))
                parts.Add(districtName.Trim());

            if (!string.IsNullOrWhiteSpace(provinceName))
                parts.Add(provinceName.Trim());

            return string.Join(", ", parts);
        }

        public async Task<ResponseDTO> CreateShipmentAsync(Guid orderId)
        {
            const string provider = "GHN";
            const int codAmount = 0;
            const string note = "Tạo vận đơn từ seller";

            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.Shipment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                        .ThenInclude(b => b.Listing)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            if (order.Shipment != null)
                return Fail(BusinessCode.INVALID_ACTION, "Order đã có shipment");

            if (order.Status != OrderStatusEnum.Confirmed)
                return Fail(BusinessCode.INVALID_ACTION, "Seller chưa confirm đơn");

            if (string.IsNullOrWhiteSpace(order.ReceiverName))
                return Fail(BusinessCode.INVALID_DATA, "Order thiếu tên người nhận");

            if (string.IsNullOrWhiteSpace(order.ReceiverPhone))
                return Fail(BusinessCode.INVALID_DATA, "Order thiếu số điện thoại người nhận");

            if (string.IsNullOrWhiteSpace(order.ReceiverAddress))
                return Fail(BusinessCode.INVALID_DATA, "Order thiếu địa chỉ người nhận");

            if (order.ToDistrictId <= 0)
                return Fail(BusinessCode.INVALID_DATA, "Order thiếu ToDistrictId");

            if (string.IsNullOrWhiteSpace(order.ToWardCode))
                return Fail(BusinessCode.INVALID_DATA, "Order thiếu ToWardCode");

            var firstBike = order.OrderItems.FirstOrDefault()?.Bike;

            if (firstBike?.Listing == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy thông tin listing của order");


            int weight = (int)(firstBike.Weight * 1000);

            if (firstBike.Weight <= 0)
                return Fail(BusinessCode.INVALID_DATA, "Weight không hợp lệ");


            var sellerId = firstBike.Listing.UserId;

            var profile = await _sellerProfileRepo.AsQueryable()
                .FirstOrDefaultAsync(p => p.UserId == sellerId && p.IsDefault);

            if (profile == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Seller chưa có địa chỉ gửi hàng mặc định");

            if (string.IsNullOrWhiteSpace(profile.SenderName))
                return Fail(BusinessCode.INVALID_DATA, "Seller chưa có tên người gửi");

            if (string.IsNullOrWhiteSpace(profile.SenderPhone))
                return Fail(BusinessCode.INVALID_DATA, "Seller chưa có số điện thoại người gửi");

            if (string.IsNullOrWhiteSpace(profile.SenderAddress))
                return Fail(BusinessCode.INVALID_DATA, "Seller chưa có địa chỉ gửi hàng");

            if (profile.FromDistrictId <= 0)
                return Fail(BusinessCode.INVALID_DATA, "Seller chưa có FromDistrictId hợp lệ");

            if (string.IsNullOrWhiteSpace(profile.FromWardCode))
                return Fail(BusinessCode.INVALID_DATA, "Seller chưa có FromWardCode hợp lệ");



            var feeResult = await _shippingProviderClient.CalculateFeeAsync(
    provider: provider,
    fromDistrictId: profile.FromDistrictId,
    fromWardCode: profile.FromWardCode.Trim(),
    toDistrictId: (int)order.ToDistrictId,
    toWardCode: order.ToWardCode.Trim(),
    insuranceValue: 0,
    weight: weight
);

            if (!feeResult.IsSuccess)
                return Fail(BusinessCode.INVALID_ACTION, feeResult.ErrorMessage ?? "Không tính được phí ship");

            order.ShippingFee = feeResult.Fee;


            var providerResult = await _shippingProviderClient.CreateOrderAsync(
     provider: provider,

     senderName: profile.SenderName.Trim(),
     senderPhone: profile.SenderPhone.Trim(),
     senderAddress: profile.SenderAddress.Trim(),
     fromDistrictId: profile.FromDistrictId,
     fromWardCode: profile.FromWardCode.Trim(),

     receiverName: order.ReceiverName.Trim(),
     receiverPhone: order.ReceiverPhone.Trim(),
     receiverAddress: order.ReceiverAddress.Trim(),
     toDistrictId: (int)order.ToDistrictId,
     toWardCode: order.ToWardCode.Trim(),

     codAmount: codAmount,
     note: note,
     weight: weight // 👈 FIX CHỖ NÀY
 );

            if (!providerResult.IsSuccess)
            {
                return Fail(
                    BusinessCode.INVALID_ACTION,
                    providerResult.ErrorMessage ?? "Tạo đơn vận chuyển thất bại");
            }

            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ShippingProvider = provider,
                ShipmentCode = GenerateShipmentCode(),
                ProviderOrderCode = providerResult.ProviderOrderCode,
                Status = ShipmentStatusEnum.Created,

                ShippingFee = order.ShippingFee,

                SenderName = profile.SenderName.Trim(),
                SenderPhone = profile.SenderPhone.Trim(),
                SenderAddress = profile.SenderAddress.Trim(),

                ReceiverName = order.ReceiverName.Trim(),
                ReceiverPhone = order.ReceiverPhone.Trim(),
                ReceiverAddress = order.ReceiverAddress.Trim(),

                Note = note
            };

            await _shipmentRepo.Insert(shipment);

            var createdLocation = BuildLocation(
                profile.FromWardName,
                profile.FromDistrictName,
                profile.FromProvinceName,
                profile.SenderAddress);

            await _trackingRepo.Insert(new ShipmentTracking
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                Status = ShipmentStatusEnum.Created,
                Title = "Tạo vận đơn thành công",
                Description = "Đơn vận chuyển đã được tạo trên GHN",
                Location = createdLocation,
                EventTime = DateTime.UtcNow,
                RawStatus = "created"
            });

            order.Status = OrderStatusEnum.Shipping;

            await _uow.SaveChangeAsync();

            return Success(new
            {
                ShipmentId = shipment.Id,
                OrderId = shipment.OrderId,
                ShipmentCode = shipment.ShipmentCode,
                ProviderOrderCode = shipment.ProviderOrderCode,
                TrackingUrl = providerResult.TrackingUrl,
                ShippingProvider = shipment.ShippingProvider,
                ShippingFee = shipment.ShippingFee,
                Status = shipment.Status.ToString(),

                SenderName = shipment.SenderName,
                SenderPhone = shipment.SenderPhone,
                SenderAddress = shipment.SenderAddress,

                ReceiverName = shipment.ReceiverName,
                ReceiverPhone = shipment.ReceiverPhone,
                ReceiverAddress = shipment.ReceiverAddress,

                OrderStatus = order.Status.ToString()
            }, BusinessCode.CREATED_SUCCESSFULLY);
        }
        public async Task<ResponseDTO> GetShipmentByOrderIdAsync(Guid orderId)
        {
            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            var shipment = await _shipmentRepo.GetFirstByExpression(
                s => s.OrderId == orderId,
                s => s.Trackings);

            if (shipment == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy shipment");

            string? trackingUrl = null;

            if (!string.IsNullOrWhiteSpace(shipment.ProviderOrderCode))
            {
                trackingUrl = shipment.ShippingProvider.ToUpper() switch
                {
                    "GHN" => $"https://donhang.ghn.vn/?order_code={shipment.ProviderOrderCode}",
                    _ => null
                };
            }

            var dto = new ShipmentDetailDTO
            {
                ShipmentId = shipment.Id,
                OrderId = shipment.OrderId,
                ShippingProvider = shipment.ShippingProvider,
                ShipmentCode = shipment.ShipmentCode,
                ProviderOrderCode = shipment.ProviderOrderCode,
                TrackingUrl = trackingUrl, // 👈 thêm dòng này
                Status = shipment.Status.ToString(),
                ShippingFee = shipment.ShippingFee,

                SenderName = shipment.SenderName,
                SenderPhone = shipment.SenderPhone,
                SenderAddress = shipment.SenderAddress,

                // ⚠ nếu Shipment entity chưa lưu mấy field này thì để null
                FromProvinceId = null,
                FromDistrictId = null,
                FromWardCode = null,
                FromProvinceName = null,
                FromDistrictName = null,
                FromWardName = null,

                ReceiverName = shipment.ReceiverName,
                ReceiverPhone = shipment.ReceiverPhone,
                ReceiverAddress = shipment.ReceiverAddress,

                // ⚠ nếu Shipment entity chưa lưu mấy field này thì để null
                ToProvinceId = null,
                ToDistrictId = null,
                ToWardCode = null,
                ToProvinceName = null,
                ToDistrictName = null,
                ToWardName = null,

                Trackings = shipment.Trackings
                    .OrderByDescending(t => t.EventTime)
                    .Select(t => new ShipmentTrackingDTO
                    {
                        Status = t.Status.ToString(),
                        Title = t.Title,
                        Description = t.Description,
                        Location = t.Location,
                        EventTime = t.EventTime
                    }).ToList()
            };

            return Success(dto);
        }
        public async Task<ResponseDTO> SyncTrackingAsync(Guid orderId)
        {
            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            var shipment = await _shipmentRepo.GetFirstByExpression(
                s => s.OrderId == orderId,
                s => s.Trackings,
                s => s.Order);

            if (shipment == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy shipment");

            if (shipment.Order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            // nếu đã delivered rồi thì không cập nhật nữa
            if (shipment.Status == ShipmentStatusEnum.Delivered)
            {
                if (shipment.Order.Status != OrderStatusEnum.Delivered)
                {
                    shipment.Order.Status = OrderStatusEnum.Delivered;
                    await _orderRepo.Update(shipment.Order);
                    await _uow.SaveChangeAsync();
                }

                return Success(new
                {
                    ShipmentId = shipment.Id,
                    ShipmentStatus = shipment.Status.ToString(),
                    OrderStatus = shipment.Order.Status.ToString(),
                    Message = "Shipment đã ở trạng thái Delivered"
                }, BusinessCode.UPDATE_SUCESSFULLY);
            }

            var latestTracking = shipment.Trackings
                .OrderByDescending(t => t.EventTime)
                .FirstOrDefault();

            var deliveredDescription = "Fake sync: giao hàng thành công";
            var deliveredLocation = shipment.ReceiverAddress ?? "Địa chỉ người nhận";

            var isDuplicate = latestTracking != null
                && latestTracking.Status == ShipmentStatusEnum.Delivered
                && string.Equals(latestTracking.Description ?? "", deliveredDescription, StringComparison.Ordinal)
                && string.Equals(latestTracking.Location ?? "", deliveredLocation, StringComparison.Ordinal);

            if (!isDuplicate)
            {
                await _trackingRepo.Insert(new ShipmentTracking
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment.Id,
                    Status = ShipmentStatusEnum.Delivered,
                    Title = "Đã giao hàng",
                    Description = deliveredDescription,
                    Location = deliveredLocation,
                    EventTime = DateTime.UtcNow,
                    RawStatus = "delivered"
                });
            }

            shipment.Status = ShipmentStatusEnum.Delivered;
            shipment.DeliveredAt ??= DateTime.UtcNow;

            shipment.Order.Status = OrderStatusEnum.Delivered;
            await _orderRepo.Update(shipment.Order);

            await _shipmentRepo.Update(shipment);
            await _uow.SaveChangeAsync();

            return Success(new
            {
                ShipmentId = shipment.Id,
                ShipmentStatus = shipment.Status.ToString(),
                OrderStatus = shipment.Order.Status.ToString(),
                Description = deliveredDescription,
                Location = deliveredLocation
            }, BusinessCode.UPDATE_SUCESSFULLY);
        }
        public async Task<ResponseDTO> ConfirmReceivedAsync(Guid buyerId, Guid orderId)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.Shipment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == buyerId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            if (order.Shipment == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Order chưa có shipment");

            if (order.Status != OrderStatusEnum.Delivered)
                return Fail(BusinessCode.INVALID_ACTION, "Order chưa ở trạng thái Delivered");

            if (order.Shipment.Status != ShipmentStatusEnum.Delivered)
                return Fail(BusinessCode.INVALID_ACTION, "Shipment chưa giao thành công");

            order.Status = OrderStatusEnum.Completed;
            order.CompletedAt = DateTime.UtcNow;

            foreach (var item in order.OrderItems)
            {
                if (item.Bike != null)
                    item.Bike.Status = BikeStatusEnum.Sold;
            }

            await _uow.SaveChangeAsync();

            return Success(new
            {
                OrderId = order.Id,
                Status = order.Status.ToString()
            }, BusinessCode.UPDATE_SUCESSFULLY);
        }
    }
}