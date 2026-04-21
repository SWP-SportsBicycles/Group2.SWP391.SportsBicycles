using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class PaymentService : IPaymentService
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Transaction> _transactionRepo;
        private readonly IUnitOfWork _uow;
        private readonly IPayOSService _payOSService;
        private readonly IShipmentService _shipmentService;

        public PaymentService(
     IGenericRepository<Order> orderRepo,
     IGenericRepository<Transaction> transactionRepo,
     IUnitOfWork uow,
     IPayOSService payOSService,
     IShipmentService shipmentService) // 🔥 thêm
        {
            _orderRepo = orderRepo;
            _transactionRepo = transactionRepo;
            _uow = uow;
            _payOSService = payOSService;
            _shipmentService = shipmentService;
        }

        public async Task<ResponseDTO> CreatePaymentLink(Guid buyerId, Guid orderId)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.Transaction)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == buyerId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Order không tồn tại hoặc không thuộc về bạn");

            if (order.Status == OrderStatusEnum.Paid)
                return Fail(BusinessCode.INVALID_ACTION, "Order đã thanh toán");

            if (order.Status != OrderStatusEnum.Locked &&
                order.Status != OrderStatusEnum.Pending)
                return Fail(BusinessCode.INVALID_ACTION, "Order không hợp lệ để tạo thanh toán");

            // Đã có transaction -> xử lý theo trạng thái
            if (order.Transaction != null)
            {
                if (order.Transaction.Status == TransactionStatusEnum.Paid)
                    return Fail(BusinessCode.INVALID_ACTION, "Đơn hàng đã được thanh toán");

                if (order.Transaction.Status == TransactionStatusEnum.Pending &&
                    !string.IsNullOrWhiteSpace(order.Transaction.PaymentLink))
                {
                    return Success(new
                    {
                        paymentUrl = order.Transaction.PaymentLink,
                        reused = true
                    });
                }

                if (order.Transaction.Status == TransactionStatusEnum.Failed ||
                    order.Transaction.Status == TransactionStatusEnum.Refunded)
                {
                    long newProviderOrderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    var newPaymentLink = await _payOSService.CreatePaymentLink(
                        newProviderOrderCode,
                        (int)order.TotalAmount
                    );

                    order.Transaction.ProviderOrderCode = newProviderOrderCode.ToString();
                    order.Transaction.PaymentLink = newPaymentLink;
                    order.Transaction.Status = TransactionStatusEnum.Pending;
                    order.Transaction.Amount = order.TotalAmount;
                    order.Transaction.PaidAt = null;
                    order.Transaction.Description = "Tạo lại link thanh toán";

                    await _uow.SaveChangeAsync();

                    return Success(new
                    {
                        paymentUrl = newPaymentLink,
                        reused = false,
                        recreated = true
                    });
                }
            }

            // Chưa có transaction -> tạo mới
            var internalOrderCode = $"ORD-{DateTime.UtcNow.Ticks}";
            long providerOrderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var paymentLink = await _payOSService.CreatePaymentLink(
                providerOrderCode,
                (int)order.TotalAmount
            );

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                OrderCode = internalOrderCode,
                ProviderOrderCode = providerOrderCode.ToString(),
                PaymentLink = paymentLink,
                Status = TransactionStatusEnum.Pending,
                Amount = order.TotalAmount,
                UserId = order.UserId,
                Description = "Tạo link thanh toán PayOS"
            };

            await _transactionRepo.Insert(transaction);
            await _uow.SaveChangeAsync();

            return Success(new
            {
                paymentUrl = paymentLink,
                reused = false
            });
        }


        public async Task<ResponseDTO> HandlePaymentSuccessAsync(string providerOrderCode)
        {
            if (string.IsNullOrWhiteSpace(providerOrderCode))
                return Fail(BusinessCode.INVALID_INPUT, "ProviderOrderCode không hợp lệ");

            var transaction = await _transactionRepo.AsQueryable()
                .Include(t => t.Order)
                    .ThenInclude(o => o.Shipment)
                .Include(t => t.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Bike)
                            .ThenInclude(b => b.Listing)
                                .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(t => t.ProviderOrderCode == providerOrderCode);

            if (transaction == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy transaction");

            if (transaction.Status == TransactionStatusEnum.Paid)
            {
                return Success(new
                {
                    message = "Đã xử lý trước đó",
                    orderId = transaction.OrderId
                });
            }

            if (transaction.Order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            var order = transaction.Order;

            // ❗ CHẶN nếu đã có người thanh toán trước
            if (order.Status == OrderStatusEnum.Paid)
            {
                transaction.Status = TransactionStatusEnum.Failed;
                transaction.Description = "Thanh toán bị từ chối vì order đã được thanh toán trước đó";

                await _uow.SaveChangeAsync();

                return Fail(BusinessCode.INVALID_ACTION, "Order đã được thanh toán rồi");
            }

            // ✅ Cho phép thanh toán
            transaction.Status = TransactionStatusEnum.Paid;
            transaction.PaidAt = DateTime.UtcNow;

            if (order.Status == OrderStatusEnum.Locked || order.Status == OrderStatusEnum.Pending)
                order.Status = OrderStatusEnum.Paid;

            await _uow.SaveChangeAsync();

            // ❌ KHÔNG tạo shipment ở đây nữa
            return Success(new
            {
                message = "Thanh toán thành công, chờ seller confirm",
                orderId = order.Id
            });


          
        }
        private static ResponseDTO Success(object? data = null)
            => new()
            {
                IsSucess = true,
                BusinessCode = BusinessCode.CREATED_SUCCESSFULLY,
                Data = data
            };

        private static ResponseDTO Fail(BusinessCode code, string msg)
            => new()
            {
                IsSucess = false,
                BusinessCode = code,
                Message = msg
            };
    }
}
