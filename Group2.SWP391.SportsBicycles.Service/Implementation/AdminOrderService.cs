using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
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
    public class AdminOrderService : IAdminOrderService
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _uow;

        public AdminOrderService(
            IGenericRepository<Order> orderRepo,
            IEmailService emailService,
            IUnitOfWork uow)
        {
            _orderRepo = orderRepo;
            _emailService = emailService;
            _uow = uow;
        }
        private static ResponseDTO Success(object? data = null)
           => new() { IsSucess = true, BusinessCode = BusinessCode.GET_DATA_SUCCESSFULLY, Data = data };

        private static ResponseDTO Fail(string msg)
            => new() { IsSucess = false, BusinessCode = BusinessCode.INVALID_ACTION, Message = msg };

        public async Task<ResponseDTO> GetOrdersAsync(int page, int size, OrderStatusEnum? status)
        {
            IQueryable<Order> query = _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                 .ThenInclude(oi => oi.Bike)
                .ThenInclude(b => b.Listing)
               .ThenInclude(l => l.User);

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            query = query.OrderByDescending(o => o.CreatedAt);

            var total = await query.CountAsync();

            var orders = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var data = orders.Select(o =>
            {
                var bike = o.OrderItems.First().Bike;

                return new AdminOrderListDTO
                {
                    OrderId = o.Id,
                    Status = o.Status.ToString(),
                    TotalAmount = o.TotalAmount,
                    BikeTitle = bike.Listing.Title,
                    SellerName = bike.Listing.User.FullName,
                    CompletedAt = o.CompletedAt,
                    PaidOutAt = o.PaidOutAt
                };
            });

            return Success(new
            {
                Items = data,
                TotalItems = total,
                Page = page,
                Size = size,
                TotalPages = (int)Math.Ceiling(total / (double)size)
            });
        }

        public async Task<ResponseDTO> NotifySellerAsync(Guid orderId)
        {
            try
            {
                var order = await _orderRepo.AsQueryable()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Bike)
                            .ThenInclude(b => b.Listing)
                                .ThenInclude(l => l.User)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return Fail("Không tìm thấy order");

                if (order.Status != OrderStatusEnum.Completed)
                    return Fail("Chỉ đơn đã giao thành công mới được notify");

                if (order.SellerNotifiedAt != null)
                    return Fail("Đã gửi mail trước đó");

                var bike = order.OrderItems.First().Bike;
                var seller = bike.Listing.User;

                // ===== CALCULATE TIỀN =====
                decimal originalPrice = bike.OriginalPrice;
                decimal commission = originalPrice * 0.05m;
                decimal inspectionFee = 100000;
                decimal payout = originalPrice - commission - inspectionFee;

                // ===== EMAIL =====
                string subject = "📦 Đơn hàng đã giao thành công";

                string body = $@"
<html>
<body style='font-family:Arial;background:#f4f6f8;padding:20px'>
<div style='max-width:600px;margin:auto;background:white;border-radius:10px'>

<div style='background:#28a745;color:white;padding:15px;font-size:18px'>
Đơn hàng hoàn tất
</div>

<div style='padding:20px'>
<p>Xin chào <strong>{seller.FullName}</strong>,</p>

<p>Đơn hàng của bạn đã được giao thành công 🎉</p>

<div style='background:#f8f9fa;padding:15px;border-radius:8px'>
<b>Mã đơn:</b> {order.Id}<br/>
<b>Sản phẩm:</b> {bike.Listing.Title}<br/>
<b>Giá bán:</b> {order.TotalAmount:N0} VNĐ
</div>

<p style='margin-top:15px'>💰 Số tiền sẽ giải ngân:</p>

<div style='background:#e9f7ef;padding:15px;border-radius:8px;color:#155724'>
<b>{payout:N0} VNĐ</b><br/>
(Đã trừ 5% phí sàn + 100.000 VNĐ phí kiểm định)
</div>

<p style='margin-top:15px'>
⏳ Hệ thống sẽ tiến hành giải ngân sau <b>48 giờ</b>.
</p>

</div>
</div>
</body>
</html>";

                await _emailService.SendEmailAsync(seller.Email, subject, body);

                // ===== UPDATE =====
                order.SellerNotifiedAt = DateTime.UtcNow;
                order.PayoutEligibleAt = DateTime.UtcNow.AddHours(48);
                await _uow.SaveChangeAsync();

                return Success(null);
            }
            catch (Exception ex)
            {
                return Fail("Lỗi notify: " + ex.Message);
            }
        }

        public async Task<ResponseDTO> ConfirmPayoutAsync(Guid orderId)
        {
            try
            {
                var order = await _orderRepo.AsQueryable()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Bike)
                            .ThenInclude(b => b.Listing)
                                .ThenInclude(l => l.User)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return Fail("Không tìm thấy order");

                if (order.Status != OrderStatusEnum.Completed)
                    return Fail("Đơn chưa hoàn tất");

                if (order.SellerNotifiedAt == null)
                    return Fail("Chưa notify seller");

                if (order.PaidOutAt != null)
                    return Fail("Đã giải ngân trước đó");

                // 🔥 CHECK 48H
                //if (order.PayoutEligibleAt == null || order.PayoutEligibleAt > DateTime.UtcNow)
                //    return Fail("Chưa đủ 48h để giải ngân");

                var bike = order.OrderItems.First().Bike;
                var seller = bike.Listing.User;

                // ===== CALCULATE =====
                decimal originalPrice = bike.OriginalPrice;
                decimal commission = originalPrice * 0.05m;
                decimal inspectionFee = 100000;
                decimal payout = originalPrice - commission - inspectionFee;

                // ===== EMAIL =====
                string subject = "💸 Đã giải ngân thành công";

                string body = $@"
<html>
<body style='font-family:Arial;background:#f4f6f8;padding:20px'>
<div style='max-width:600px;margin:auto;background:white;border-radius:10px'>

<div style='background:#007bff;color:white;padding:15px;font-size:18px'>
Giải ngân thành công
</div>

<div style='padding:20px'>
<p>Xin chào <strong>{seller.FullName}</strong>,</p>

<p>Hệ thống đã chuyển tiền thành công cho đơn hàng:</p>

<div style='background:#f8f9fa;padding:15px;border-radius:8px'>
<b>Mã đơn:</b> {order.Id}<br/>
<b>Sản phẩm:</b> {bike.Listing.Title}
</div>

<p style='margin-top:15px'>💰 Số tiền đã nhận:</p>

<div style='background:#d4edda;padding:15px;border-radius:8px;color:#155724'>
<b>{payout:N0} VNĐ</b>
</div>

<p style='margin-top:15px'>
Tiền đã được chuyển về tài khoản ngân hàng bạn đã đăng ký.
</p>

</div>
</div>
</body>
</html>";

                await _emailService.SendEmailAsync(seller.Email, subject, body);

                // ===== UPDATE =====
                order.PaidOutAt = DateTime.UtcNow;

                await _uow.SaveChangeAsync();

                return Success(null);
            }
            catch (Exception ex)
            {
                return Fail("Lỗi payout: " + ex.Message);
            }
        }
    }
}
