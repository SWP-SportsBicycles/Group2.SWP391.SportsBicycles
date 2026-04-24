using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class PaymentService : IPaymentService
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Transaction> _transactionRepo;
        private readonly IGenericRepository<RefundInfo> _refundInfoRepo;
        private readonly IUnitOfWork _uow;
        private readonly IPayOSService _payOSService;
        private readonly IShipmentService _shipmentService;

        public PaymentService(
            IGenericRepository<Order> orderRepo,
            IGenericRepository<Transaction> transactionRepo,
            IGenericRepository<RefundInfo> refundInfoRepo,
            IUnitOfWork uow,
            IPayOSService payOSService,
            IShipmentService shipmentService)
        {
            _orderRepo = orderRepo;
            _transactionRepo = transactionRepo;
            _refundInfoRepo = refundInfoRepo;
            _uow = uow;
            _payOSService = payOSService;
            _shipmentService = shipmentService;
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

        private async Task ReleaseOrderAndBikesAsync(Order order, string reason)
        {
            order.Status = OrderStatusEnum.Cancelled;

            if (order.Transaction != null &&
                order.Transaction.Status == TransactionStatusEnum.Pending)
            {
                order.Transaction.Status = TransactionStatusEnum.Failed;
                order.Transaction.Description = reason;
            }

            foreach (var item in order.OrderItems)
            {
                if (item.Bike != null &&
                    item.Bike.Status == BikeStatusEnum.Reserved)
                {
                    item.Bike.Status = BikeStatusEnum.Available;
                }
            }

            await _uow.SaveChangeAsync();
        }

        public async Task<ResponseDTO> CreatePaymentLink(Guid buyerId, Guid orderId)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.Transaction)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == buyerId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Order không tồn tại hoặc không thuộc về bạn");

            if (order.Status == OrderStatusEnum.Locked &&
                order.ExpiresAt != null &&
                order.ExpiresAt <= DateTime.UtcNow)
            {
                await ReleaseOrderAndBikesAsync(order, "Order expired before creating PayOS payment link");

                return Fail(BusinessCode.INVALID_ACTION,
                    "Order đã hết thời gian giữ chỗ, vui lòng tạo lại đơn hàng");
            }

            if (order.Status == OrderStatusEnum.Paid)
                return Fail(BusinessCode.INVALID_ACTION, "Order đã thanh toán");

            if (order.Status != OrderStatusEnum.Locked &&
                order.Status != OrderStatusEnum.Pending)
                return Fail(BusinessCode.INVALID_ACTION, "Order không hợp lệ để tạo thanh toán");

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

            if (order.Status == OrderStatusEnum.Cancelled)
            {
                transaction.Status = TransactionStatusEnum.Failed;
                transaction.Description = "Thanh toán bị từ chối vì order đã bị hủy hoặc hết hạn";

                foreach (var item in order.OrderItems)
                {
                    if (item.Bike != null &&
                        item.Bike.Status == BikeStatusEnum.Reserved)
                    {
                        item.Bike.Status = BikeStatusEnum.Available;
                    }
                }

                await _uow.SaveChangeAsync();

                return Fail(BusinessCode.INVALID_ACTION,
                    "Order đã hết hạn hoặc đã bị hủy, không thể xác nhận thanh toán");
            }

            if (order.Status == OrderStatusEnum.Locked &&
                order.ExpiresAt != null &&
                order.ExpiresAt <= DateTime.UtcNow)
            {
                transaction.Status = TransactionStatusEnum.Failed;
                transaction.Description = "Thanh toán bị từ chối vì order đã hết hạn";

                order.Status = OrderStatusEnum.Cancelled;

                foreach (var item in order.OrderItems)
                {
                    if (item.Bike != null &&
                        item.Bike.Status == BikeStatusEnum.Reserved)
                    {
                        item.Bike.Status = BikeStatusEnum.Available;
                    }
                }

                await _uow.SaveChangeAsync();

                return Fail(BusinessCode.INVALID_ACTION,
                    "Order đã hết thời gian giữ chỗ, vui lòng tạo lại đơn hàng");
            }

            if (order.Status == OrderStatusEnum.Paid)
            {
                transaction.Status = TransactionStatusEnum.Failed;
                transaction.Description = "Thanh toán bị từ chối vì order đã được thanh toán trước đó";

                await _uow.SaveChangeAsync();

                return Fail(BusinessCode.INVALID_ACTION, "Order đã được thanh toán rồi");
            }

            transaction.Status = TransactionStatusEnum.Paid;
            transaction.PaidAt = DateTime.UtcNow;

            if (order.Status == OrderStatusEnum.Locked || order.Status == OrderStatusEnum.Pending)
                order.Status = OrderStatusEnum.Paid;

            await _uow.SaveChangeAsync();

            return Success(new
            {
                message = "Thanh toán thành công, chờ seller confirm",
                orderId = order.Id
            });
        }

        public async Task<ResponseDTO> CancelOrderAsync(Guid buyerId, Guid orderId, string? reason)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.Transaction)
                .Include(o => o.Shipment)
                .Include(o => o.RefundInfo)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == buyerId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            if (order.Transaction == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không có transaction để hoàn tiền");

            decimal penaltyPercent;
            decimal shippingLoss;

            switch (order.Status)
            {
                case OrderStatusEnum.Paid:
                    penaltyPercent = 0.05m;
                    shippingLoss = order.Shipment == null ? 0m : order.ShippingFee;
                    break;

                case OrderStatusEnum.Delivered:
                    penaltyPercent = 0.10m;
                    shippingLoss = order.ShippingFee;
                    break;

                default:
                    return Fail(BusinessCode.INVALID_ACTION,
                        "Chỉ được hủy đơn khi đang ở trạng thái Paid hoặc Delivered");
            }

            var penaltyAmount = order.SubTotal * penaltyPercent;
            var refundAmount = order.TotalAmount - penaltyAmount - shippingLoss;

            if (refundAmount < 0)
                refundAmount = 0;

            order.Status = OrderStatusEnum.Cancelled;
            order.Transaction.Status = TransactionStatusEnum.RefundPending;
            order.Transaction.Description =
                $"Buyer cancel order | Reason: {reason ?? "Không có"} | " +
                $"PenaltyPercent: {penaltyPercent * 100}% | " +
                $"PenaltyAmount: {penaltyAmount} | " +
                $"ShippingLoss: {shippingLoss} | " +
                $"RefundAmount: {refundAmount}";

            if (order.RefundInfo != null)
            {
                order.RefundInfo.RefundAmount = refundAmount;
                order.RefundInfo.Note = reason;
            }

            await _uow.SaveChangeAsync();

            return Success(new
            {
                orderId = order.Id,
                orderStatus = order.Status.ToString(),
                transactionStatus = order.Transaction.Status.ToString(),
                subTotal = order.SubTotal,
                shippingFee = order.ShippingFee,
                totalAmount = order.TotalAmount,
                penaltyPercent = penaltyPercent * 100,
                penaltyAmount,
                shippingLoss,
                refundAmount,
                message = "Đã hủy đơn, vui lòng cung cấp thông tin tài khoản để hoàn tiền"
            });
        }

        public async Task<ResponseDTO> SubmitRefundInfoAsync(Guid buyerId, Guid orderId, RefundInfoDTO dto)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            if (dto == null)
                return Fail(BusinessCode.INVALID_INPUT, "Dữ liệu hoàn tiền không hợp lệ");

            if (string.IsNullOrWhiteSpace(dto.BankName))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu tên ngân hàng");

            if (string.IsNullOrWhiteSpace(dto.BankAccountNumber))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu số tài khoản");

            if (string.IsNullOrWhiteSpace(dto.BankAccountName))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu tên chủ tài khoản");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.Transaction)
                .Include(o => o.RefundInfo)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == buyerId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            if (order.Status != OrderStatusEnum.Cancelled)
                return Fail(BusinessCode.INVALID_ACTION, "Order chưa bị hủy");

            if (order.Transaction == null || order.Transaction.Status != TransactionStatusEnum.RefundPending)
                return Fail(BusinessCode.INVALID_ACTION, "Order không ở trạng thái chờ hoàn tiền");

            decimal penaltyPercent = order.Transaction.Description?.Contains("PenaltyPercent: 10") == true ? 0.10m : 0.05m;

            decimal shippingLoss = order.Status == OrderStatusEnum.Cancelled && order.Shipment != null
                ? order.ShippingFee
                : 0m;

            var penaltyAmount = order.SubTotal * penaltyPercent;
            var refundAmount = order.TotalAmount - penaltyAmount - shippingLoss;

            if (refundAmount < 0)
                refundAmount = 0;

            if (order.RefundInfo == null)
            {
                var refundInfo = new RefundInfo
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    UserId = buyerId,
                    BankName = dto.BankName.Trim(),
                    BankAccountNumber = dto.BankAccountNumber.Trim(),
                    BankAccountName = dto.BankAccountName.Trim(),
                    RefundAmount = refundAmount,
                    Note = dto.Note
                };

                await _refundInfoRepo.Insert(refundInfo);
            }
            else
            {
                order.RefundInfo.BankName = dto.BankName.Trim();
                order.RefundInfo.BankAccountNumber = dto.BankAccountNumber.Trim();
                order.RefundInfo.BankAccountName = dto.BankAccountName.Trim();
                order.RefundInfo.RefundAmount = refundAmount;
                order.RefundInfo.Note = dto.Note;
            }

            await _uow.SaveChangeAsync();

            return Success(new
            {
                orderId = order.Id,
                refundAmount,
                bankName = dto.BankName,
                bankAccountNumber = dto.BankAccountNumber,
                bankAccountName = dto.BankAccountName,
                message = "Đã lưu thông tin hoàn tiền"
            });
        }

        public async Task<ResponseDTO> GetRefundStatusAsync(Guid buyerId, Guid orderId)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.Transaction)
                .Include(o => o.RefundInfo)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == buyerId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            if (order.Transaction == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không có transaction");

            string refundStatus = order.Transaction.Status switch
            {
                TransactionStatusEnum.RefundPending => "Đang chờ hoàn tiền",
                TransactionStatusEnum.Refunded => "Đã hoàn tiền",
                _ => "Không có hoàn tiền"
            };

            return Success(new
            {
                orderId = order.Id,
                orderStatus = order.Status.ToString(),
                transactionStatus = order.Transaction.Status.ToString(),
                refundStatus,
                refundInfo = order.RefundInfo == null ? null : new
                {
                    order.RefundInfo.BankName,
                    order.RefundInfo.BankAccountNumber,
                    order.RefundInfo.BankAccountName,
                    order.RefundInfo.RefundAmount,
                    order.RefundInfo.Note
                },
                note = order.Transaction.Description
            });
        }

        public async Task<ResponseDTO> ConfirmRefundAsync(Guid orderId)
        {
            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.Transaction)
                .Include(o => o.RefundInfo)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            if (order.Transaction == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không có transaction");

            if (order.Transaction.Status != TransactionStatusEnum.RefundPending)
                return Fail(BusinessCode.INVALID_ACTION, "Order không ở trạng thái chờ hoàn tiền");

            if (order.RefundInfo == null)
                return Fail(BusinessCode.INVALID_ACTION, "Buyer chưa cung cấp thông tin hoàn tiền");

            order.Transaction.Status = TransactionStatusEnum.Refunded;
            order.Transaction.Description =
                (order.Transaction.Description ?? "") + " | Admin đã xác nhận hoàn tiền";

            await _uow.SaveChangeAsync();

            return Success(new
            {
                orderId = order.Id,
                transactionStatus = order.Transaction.Status.ToString(),
                refundAmount = order.RefundInfo.RefundAmount,
                message = "Đã xác nhận hoàn tiền"
            });
        }
    }
}