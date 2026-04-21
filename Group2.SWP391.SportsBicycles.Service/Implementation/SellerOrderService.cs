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
    public class SellerOrderService : ISellerOrderService
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IUnitOfWork _uow;

        public SellerOrderService(
            IGenericRepository<Order> orderRepo,
            IUnitOfWork uow)
        {
            _orderRepo = orderRepo;
            _uow = uow;
        }
        private static ResponseDTO Success(object? data = null)
       => new() { IsSucess = true, Data = data };

        private static ResponseDTO Fail(string msg)
            => new() { IsSucess = false, Message = msg };

        // ================= GET MY ORDERS =================
        public async Task<ResponseDTO> GetMyOrdersAsync(Guid sellerId, int page, int size)
        {
            var query = _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                        .ThenInclude(b => b.Listing)
                .Where(o =>
                    o.OrderItems.Any(oi =>
                        oi.Bike.Listing.UserId == sellerId
                    ))
                .OrderByDescending(o => o.CreatedAt);

            var total = await query.CountAsync();

            var orders = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var data = orders.Select(o =>
            {
                var bike = o.OrderItems.First().Bike;

                return new SellerOrderDTO
                {
                    OrderId = o.Id,
                    Status = o.Status.ToString(),
                    TotalAmount = o.TotalAmount,

                    BuyerName = o.ReceiverName,
                    BuyerPhone = o.ReceiverPhone,
                    ReceiverAddress = o.ReceiverAddress,

                    BikeName = bike.Listing.Title,
                    Price = bike.Price,

                    CreatedAt = o.CreatedAt
                };
            });

            return Success(new
            {
                Items = data,
                TotalItems = total
            });
        }

        // ================= DETAIL =================
        public async Task<ResponseDTO> GetOrderDetailAsync(Guid sellerId, Guid orderId)
        {
            var order = await _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                        .ThenInclude(b => b.Listing)
                .Include(o => o.Shipment)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return Fail("Không tìm thấy order");

            var isOwner = order.OrderItems.Any(oi =>
                oi.Bike.Listing.UserId == sellerId);

            if (!isOwner)
                return Fail("Không có quyền");

            return Success(order);
        }

        // ================= CONFIRM =================
        public async Task<ResponseDTO> ConfirmOrderAsync(Guid sellerId, Guid orderId)
        {
            var order = await _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                        .ThenInclude(b => b.Listing)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return Fail("Không tìm thấy order");

            var isOwner = order.OrderItems.Any(oi =>
                oi.Bike.Listing.UserId == sellerId);

            if (!isOwner)
                return Fail("Không có quyền");

            if (order.Status != OrderStatusEnum.Paid)
                return Fail("Chỉ order Paid mới được confirm");

            order.Status = OrderStatusEnum.Confirmed;

            await _uow.SaveChangeAsync();

            return Success(new
            {
                orderId = order.Id,
                status = order.Status.ToString()
            });
        }

        // ================= CANCEL =================
        public async Task<ResponseDTO> CancelOrderAsync(Guid sellerId, Guid orderId)
        {
            var order = await _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                        .ThenInclude(b => b.Listing)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return Fail("Không tìm thấy order");

            var isOwner = order.OrderItems.Any(oi =>
                oi.Bike.Listing.UserId == sellerId);

            if (!isOwner)
                return Fail("Không có quyền");

            if (order.Status != OrderStatusEnum.Pending &&
                order.Status != OrderStatusEnum.Paid)
                return Fail("Không thể hủy order");

            order.Status = OrderStatusEnum.Cancelled;

            // 🔥 trả lại bike
            foreach (var item in order.OrderItems)
            {
                if (item.Bike != null)
                    item.Bike.Status = BikeStatusEnum.Available;
            }

            await _uow.SaveChangeAsync();

            return Success(new
            {
                orderId = order.Id,
                status = order.Status.ToString()
            });
        }
    }
}
