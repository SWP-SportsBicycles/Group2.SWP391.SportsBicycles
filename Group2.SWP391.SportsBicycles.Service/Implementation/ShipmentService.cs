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

        public ShipmentService(
            IGenericRepository<Order> orderRepo,
            IGenericRepository<Shipment> shipmentRepo,
            IGenericRepository<ShipmentTracking> trackingRepo,
            IUnitOfWork uow,
            IShippingProviderClient shippingProviderClient)
        {
            _orderRepo = orderRepo;
            _shipmentRepo = shipmentRepo;
            _trackingRepo = trackingRepo;
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

        public async Task<ResponseDTO> CreateShipmentAsync(Guid orderId, CreateShipmentDTO dto)
        {
            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            if (dto == null)
                return Fail(BusinessCode.INVALID_INPUT, "Dữ liệu không hợp lệ");

            if (string.IsNullOrWhiteSpace(dto.ShippingProvider))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu đơn vị vận chuyển");

            if (string.IsNullOrWhiteSpace(dto.SenderName))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu tên người gửi");

            if (string.IsNullOrWhiteSpace(dto.SenderPhone))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu số điện thoại người gửi");

            if (string.IsNullOrWhiteSpace(dto.SenderAddress))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu địa chỉ người gửi");

            if (dto.DistanceKm <= 0)
                return Fail(BusinessCode.INVALID_DATA, "Khoảng cách phải lớn hơn 0");

            if (dto.FromDistrictId <= 0)
                return Fail(BusinessCode.INVALID_DATA, "Thiếu FromDistrictId");

            if (dto.ToDistrictId <= 0)
                return Fail(BusinessCode.INVALID_DATA, "Thiếu ToDistrictId");

            if (string.IsNullOrWhiteSpace(dto.FromWardCode))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu FromWardCode");

            if (string.IsNullOrWhiteSpace(dto.ToWardCode))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu ToWardCode");

            var order = await _orderRepo.GetFirstByExpression(
                o => o.Id == orderId,
                o => o.Shipment);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            if (order.Shipment != null)
                return Fail(BusinessCode.INVALID_ACTION, "Order đã có shipment");

            if (order.Status != OrderStatusEnum.Confirmed)
            {
                return Fail(BusinessCode.INVALID_ACTION, "Seller chưa confirm đơn");
            }

            var shippingFee = order.ShippingFee > 0
                ? order.ShippingFee
                : ShippingFeeCalculator.Calculate(dto.DistanceKm);


            var providerResult = await _shippingProviderClient.CreateOrderAsync(
                provider: dto.ShippingProvider.Trim(),
                senderName: dto.SenderName.Trim(),
                senderPhone: dto.SenderPhone.Trim(),
                senderAddress: dto.SenderAddress.Trim(),
                fromDistrictId: dto.FromDistrictId,
                fromWardCode: dto.FromWardCode,

                receiverName: order.ReceiverName,
                receiverPhone: order.ReceiverPhone,
                receiverAddress: order.ReceiverAddress,
                toDistrictId: dto.ToDistrictId,
                toWardCode: dto.ToWardCode,

                codAmount: dto.CodAmount,
                note: dto.Note
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
                ShippingProvider = dto.ShippingProvider.Trim(),
                ShipmentCode = GenerateShipmentCode(),
                ProviderOrderCode = providerResult.ProviderOrderCode,
                Status = ShipmentStatusEnum.Created,

                ShippingFee = shippingFee,
                DistanceKm = dto.DistanceKm,

                SenderName = dto.SenderName.Trim(),
                SenderPhone = dto.SenderPhone.Trim(),
                SenderAddress = dto.SenderAddress.Trim(),

                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                ReceiverAddress = order.ReceiverAddress,

                Note = dto.Note
            };

            await _shipmentRepo.Insert(shipment);

            var createdLocation = BuildLocation(
                dto.FromWardName,
                dto.FromDistrictName,
                dto.FromProvinceName,
                dto.SenderAddress);

            await _trackingRepo.Insert(new ShipmentTracking
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                Status = ShipmentStatusEnum.Created,
                Title = "Tạo vận đơn thành công",
                Description = "Đơn vận chuyển đã được tạo trên hệ thống đối tác",
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
                ShippingFee = shipment.ShippingFee,
                DistanceKm = shipment.DistanceKm,
                Status = shipment.Status.ToString(),

                SenderName = shipment.SenderName,
                SenderPhone = shipment.SenderPhone,
                SenderAddress = shipment.SenderAddress,
                SenderLocation = createdLocation,

                ReceiverName = shipment.ReceiverName,
                ReceiverPhone = shipment.ReceiverPhone,
                ReceiverAddress = shipment.ReceiverAddress,
                ReceiverLocation = BuildLocation(
                    dto.ToWardName,
                    dto.ToDistrictName,
                    dto.ToProvinceName,
                    order.ReceiverAddress)
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

            if (string.IsNullOrWhiteSpace(shipment.ProviderOrderCode))
                return Fail(BusinessCode.INVALID_ACTION, "Shipment chưa có mã vận đơn của đối tác");

            var trackResult = await _shippingProviderClient.TrackOrderAsync(
                shipment.ShippingProvider,
                shipment.ProviderOrderCode);

            if (!trackResult.IsSuccess)
                return Fail(BusinessCode.INVALID_ACTION, trackResult.ErrorMessage ?? "Không lấy được tracking");

            var mappedStatus = MapRawStatus(trackResult.RawStatus);

            var latestTracking = shipment.Trackings
                .OrderByDescending(t => t.EventTime)
                .FirstOrDefault();

            var isDuplicate = latestTracking != null
                && latestTracking.Status == mappedStatus
                && string.Equals(latestTracking.Description ?? "", trackResult.Description ?? "", StringComparison.Ordinal)
                && string.Equals(latestTracking.Location ?? "", trackResult.Location ?? "", StringComparison.Ordinal);

            if (!isDuplicate)
            {
                await _trackingRepo.Insert(new ShipmentTracking
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment.Id,
                    Status = mappedStatus,
                    Title = GetTrackingTitle(mappedStatus),
                    Description = trackResult.Description,
                    Location = trackResult.Location,
                    EventTime = trackResult.EventTime ?? DateTime.UtcNow,
                    RawStatus = trackResult.RawStatus
                });
            }

            shipment.Status = mappedStatus;

            if (mappedStatus == ShipmentStatusEnum.PickingUp && shipment.PickupAt == null)
                shipment.PickupAt = DateTime.UtcNow;

            if (mappedStatus == ShipmentStatusEnum.Delivered)
            {
                shipment.DeliveredAt ??= DateTime.UtcNow;
                shipment.Order.Status = OrderStatusEnum.Delivered;
                await _orderRepo.Update(shipment.Order);
            }

            if (mappedStatus == ShipmentStatusEnum.Failed)
            {
                shipment.FailedAt ??= DateTime.UtcNow;
                shipment.FailReason = trackResult.Description;
            }

            await _shipmentRepo.Update(shipment);
            await _uow.SaveChangeAsync();

            return Success(new
            {
                ShipmentId = shipment.Id,
                ShipmentStatus = shipment.Status.ToString(),
                RawStatus = trackResult.RawStatus,
                Description = trackResult.Description,
                Location = trackResult.Location
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