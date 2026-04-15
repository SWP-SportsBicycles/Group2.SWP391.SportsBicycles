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
        private readonly IUnitOfWork _uow;

        public BuyerOrderService(
            IGenericRepository<Order> orderRepo,
            IGenericRepository<OrderItem> orderItemRepo,
            IGenericRepository<Bike> bikeRepo,
            IUnitOfWork uow)
        {
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _bikeRepo = bikeRepo;
            _uow = uow;
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
            if (dto == null)
                return Fail(BusinessCode.INVALID_INPUT, "Dữ liệu không hợp lệ");

            if (dto.BikeId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "BikeId không hợp lệ");

            if (string.IsNullOrWhiteSpace(dto.ReceiverName))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu tên người nhận");

            if (string.IsNullOrWhiteSpace(dto.ReceiverPhone))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu số điện thoại người nhận");

            if (string.IsNullOrWhiteSpace(dto.ReceiverAddress))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu địa chỉ người nhận");

            var bike = await _bikeRepo.AsQueryable()
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == dto.BikeId);

            if (bike == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy bike");

            if (bike.Listing == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy listing");

            if (bike.Listing.Status != ListingStatusEnum.Published)
                return Fail(BusinessCode.INVALID_ACTION, "Listing chưa được publish");

            if (bike.Status != BikeStatusEnum.Available)
                return Fail(BusinessCode.INVALID_ACTION, "Bike không khả dụng để đặt mua");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = buyerId,
                Status = OrderStatusEnum.Pending,
                ReceiverName = dto.ReceiverName.Trim(),
                ReceiverPhone = dto.ReceiverPhone.Trim(),
                ReceiverAddress = dto.ReceiverAddress.Trim(),
                TotalAmount = bike.Price
            };

            await _orderRepo.Insert(order);

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                BikeId = bike.Id,
                UnitPrice = bike.Price,
                LineTotal = bike.Price
            };

            await _orderItemRepo.Insert(orderItem);

            bike.Status = BikeStatusEnum.Reserved;

            await _uow.SaveChangeAsync();

            return Success(new
            {
                orderId = order.Id,
                bikeId = bike.Id,
                totalAmount = order.TotalAmount,
                status = order.Status.ToString()
            }, BusinessCode.CREATED_SUCCESSFULLY);
        }

        // ================= MARK PAID =================
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
    }
}