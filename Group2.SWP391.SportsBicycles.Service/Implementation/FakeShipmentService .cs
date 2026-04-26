using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class FakeShipmentService : IFakeShipmentService
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Shipment> _shipmentRepo;
        private readonly IGenericRepository<ShipmentTracking> _trackingRepo;
        private readonly IGenericRepository<SellerShippingProfile> _sellerProfileRepo;
        private readonly IUnitOfWork _uow;

        public FakeShipmentService(
            IGenericRepository<Order> orderRepo,
            IGenericRepository<Shipment> shipmentRepo,
            IGenericRepository<ShipmentTracking> trackingRepo,
            IGenericRepository<SellerShippingProfile> sellerProfileRepo,
            IUnitOfWork uow)
        {
            _orderRepo = orderRepo;
            _shipmentRepo = shipmentRepo;
            _trackingRepo = trackingRepo;
            _sellerProfileRepo = sellerProfileRepo;
            _uow = uow;
        }

        public async Task<ResponseDTO> CreateFakeAsync(Guid orderId)
        {
            var order = await _orderRepo.AsQueryable()
                .Include(o => o.Shipment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                        .ThenInclude(b => b.Listing)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return Fail("Không tìm thấy order");

            if (order.Shipment != null)
                return Fail("Đã có shipment");

            if (order.Status != OrderStatusEnum.Confirmed)
                return Fail("Chưa confirm");

            var sellerId = order.OrderItems.First().Bike.Listing.UserId;

            var profile = await _sellerProfileRepo.AsQueryable()
                .FirstOrDefaultAsync(x => x.UserId == sellerId && x.IsDefault);

            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ShipmentCode = $"FAKE-{DateTime.UtcNow.Ticks}",
                ProviderOrderCode = $"FAKE-{Guid.NewGuid().ToString("N")[..8]}",
                ShippingProvider = "FAKE",
                Status = ShipmentStatusEnum.Created,

                SenderName = profile.SenderName,
                SenderPhone = profile.SenderPhone,
                SenderAddress = profile.SenderAddress,

                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                ReceiverAddress = order.ReceiverAddress
            };

            await _shipmentRepo.Insert(shipment);

            await _trackingRepo.Insert(new ShipmentTracking
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                Status = ShipmentStatusEnum.Created,
                Title = "FAKE - Created",
                Description = "Tạo vận đơn giả",
                Location = profile.SenderAddress,
                EventTime = DateTime.UtcNow
            });

            // 🔥 chỗ bạn cần
            order.Status = OrderStatusEnum.Shipping;

            await _uow.SaveChangeAsync();

            return Success(new
            {
                shipment.Id,
                shipment.ProviderOrderCode,
                OrderStatus = order.Status.ToString()
            });
        }

        public async Task<ResponseDTO> DeliveredFakeAsync(Guid orderId)
        {
            var shipment = await _shipmentRepo.GetFirstByExpression(
                x => x.OrderId == orderId,
                x => x.Order,
                x => x.Trackings);

            if (shipment == null)
                return Fail("Không có shipment");

            await _trackingRepo.Insert(new ShipmentTracking
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                Status = ShipmentStatusEnum.Delivered,
                Title = "FAKE - Delivered",
                Description = "Giao hàng thành công (fake)",
                Location = shipment.ReceiverAddress,
                EventTime = DateTime.UtcNow
            });

            shipment.Status = ShipmentStatusEnum.Delivered;
            shipment.Order.Status = OrderStatusEnum.Delivered;

            await _uow.SaveChangeAsync();

            return Success(new
            {
                shipment.Id,
                shipment.Status,
                OrderStatus = shipment.Order.Status
            });
        }

        private ResponseDTO Success(object data) => new()
        {
            IsSucess = true,
            Data = data
        };

        private ResponseDTO Fail(string msg) => new()
        {
            IsSucess = false,
            Message = msg
        };
    }
}
