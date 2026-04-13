using Group2.SWP391.SportsBicycles.Services.Contract;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private string WrapLayout(string title, string content)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>{title}</title>
</head>
<body style='margin:0;padding:0;background:#f5f7fb;font-family:Arial,Helvetica,sans-serif;color:#1f2937;'>
    <div style='max-width:640px;margin:30px auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:14px;overflow:hidden;'>
        <div style='background:#111827;padding:20px 24px;'>
            <h1 style='margin:0;color:#ffffff;font-size:24px;'>SportsBicyclesStore</h1>
            <p style='margin:8px 0 0;color:#d1d5db;font-size:13px;'>Mua bán xe đạp minh bạch và an toàn</p>
        </div>

        <div style='padding:28px 24px;line-height:1.7;font-size:15px;'>
            {content}
        </div>

        <div style='padding:18px 24px;background:#f9fafb;border-top:1px solid #e5e7eb;'>
            <p style='margin:0;font-size:12px;color:#6b7280;'>
                Đây là email tự động từ hệ thống SportsBicyclesStore. Vui lòng không trả lời email này.
            </p>
        </div>
    </div>
</body>
</html>";
        }
        private static string FormatVnd(decimal amount)
            => string.Format(new CultureInfo("vi-VN"), "{0:N0} VNĐ", amount);

        public string BuildOtpEmail(string? fullName, string otp, int expiredMinutes)
        {
            var name = string.IsNullOrWhiteSpace(fullName) ? "bạn" : fullName;

            var content = $@"
<p>Xin chào <strong>{name}</strong>,</p>
<p>Bạn đang thực hiện xác thực tài khoản tại <strong>SportsBicyclesStore</strong>.</p>

<div style='margin:24px 0;text-align:center;'>
    <div style='display:inline-block;background:#111827;color:#ffffff;padding:14px 26px;
                border-radius:10px;font-size:30px;font-weight:bold;letter-spacing:6px;'>
        {otp}
    </div>
</div>

<p>Mã OTP có hiệu lực trong <strong>{expiredMinutes} phút</strong>.</p>
<p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</p>";

            return WrapLayout("Xác thực tài khoản SportsBicyclesStore", content);
        }

        public string BuildUserLockEmail(string? fullName)
        {
            var name = string.IsNullOrWhiteSpace(fullName) ? "bạn" : fullName;

            var content = $@"
<p>Xin chào <strong>{name}</strong>,</p>
<p>Tài khoản SportsBicyclesStore của bạn hiện đã bị <strong>tạm khóa</strong> bởi quản trị viên.</p>
<p>Nếu bạn cho rằng đây là nhầm lẫn, vui lòng liên hệ đội ngũ hỗ trợ để được kiểm tra.</p>";

            return WrapLayout("Thông báo khóa tài khoản", content);
        }

        public string BuildUserUnlockEmail(string? fullName)
        {
            var name = string.IsNullOrWhiteSpace(fullName) ? "bạn" : fullName;

            var content = $@"
<p>Xin chào <strong>{name}</strong>,</p>
<p>Tài khoản SportsBicyclesStore của bạn đã được <strong>mở khóa</strong>.</p>
<p>Bạn có thể đăng nhập và tiếp tục sử dụng hệ thống bình thường.</p>";

            return WrapLayout("Thông báo mở khóa tài khoản", content);
        }

        public string BuildListingRejectedEmail(string? fullName, string listingTitle, string? reason)
        {
            var name = string.IsNullOrWhiteSpace(fullName) ? "bạn" : fullName;
            var rejectReason = string.IsNullOrWhiteSpace(reason) ? "Không có ghi chú thêm." : reason;

            var content = $@"
<p>Xin chào <strong>{name}</strong>,</p>
<p>Listing <strong>{listingTitle}</strong> của bạn đã bị <strong>từ chối</strong>.</p>

<div style='margin-top:16px;padding:14px;background:#fff7ed;border:1px solid #fdba74;border-radius:10px;'>
    <strong>Lý do:</strong>
    <div style='margin-top:8px;'>{rejectReason}</div>
</div>

<p style='margin-top:16px;'>Bạn vui lòng kiểm tra lại thông tin và chỉnh sửa trước khi gửi duyệt lại.</p>";

            return WrapLayout("Listing bị từ chối", content);
        }

        public string BuildBikeRejectedEmail(string? fullName, string bikeName, string? reason)
        {
            var name = string.IsNullOrWhiteSpace(fullName) ? "bạn" : fullName;
            var rejectReason = string.IsNullOrWhiteSpace(reason) ? "Không có ghi chú thêm." : reason;

            var content = $@"
<p>Xin chào <strong>{name}</strong>,</p>
<p>Xe <strong>{bikeName}</strong> của bạn đã bị <strong>từ chối kiểm duyệt</strong>.</p>

<div style='margin-top:16px;padding:14px;background:#fff7ed;border:1px solid #fdba74;border-radius:10px;'>
    <strong>Lý do:</strong>
    <div style='margin-top:8px;'>{rejectReason}</div>
</div>

<p style='margin-top:16px;'>Bạn vui lòng cập nhật lại thông tin xe và gửi lại để được kiểm tra.</p>";

            return WrapLayout("Xe bị từ chối kiểm duyệt", content);
        }

        public string BuildPaymentSuccessEmail(string? fullName, string orderCode, decimal totalAmount)
        {
            var name = string.IsNullOrWhiteSpace(fullName) ? "bạn" : fullName;

            var content = $@"
<p>Xin chào <strong>{name}</strong>,</p>
<p>SportsBicyclesStore đã ghi nhận <strong>thanh toán thành công</strong> cho đơn hàng của bạn.</p>

<div style='margin-top:16px;padding:14px;background:#ecfdf5;border:1px solid #86efac;border-radius:10px;'>
    <div><strong>Mã đơn hàng:</strong> {orderCode}</div>
    <div style='margin-top:8px;'><strong>Tổng thanh toán:</strong> {FormatVnd(totalAmount)}</div>
</div>

<p style='margin-top:16px;'>Người bán sẽ sớm xử lý đơn hàng của bạn.</p>";

            return WrapLayout("Thanh toán đơn hàng thành công", content);
        }

        public string BuildOrderCompletedThankYouEmail(string? fullName, string orderCode)
        {
            var name = string.IsNullOrWhiteSpace(fullName) ? "bạn" : fullName;

            var content = $@"
<p>Xin chào <strong>{name}</strong>,</p>
<p>Đơn hàng <strong>{orderCode}</strong> đã được người bán xác nhận <strong>hoàn tất</strong>.</p>
<p>Cảm ơn bạn đã mua sắm tại <strong>SportsBicyclesStore</strong>.</p>
<p>Hy vọng bạn sẽ có trải nghiệm thật tốt với sản phẩm của mình.</p>";

            return WrapLayout("Cảm ơn bạn đã mua hàng tại SportsBicyclesStore", content);
        }


        public string BuildForgotPasswordEmail(string? fullName, string resetLink, int expiredMinutes)
        {
            var name = string.IsNullOrWhiteSpace(fullName) ? "bạn" : fullName;

            var content = $@"
<p>Xin chào <strong>{name}</strong>,</p>
<p>SportsBicyclesStore đã nhận được yêu cầu <strong>đặt lại mật khẩu</strong> cho tài khoản của bạn.</p>
<p>Vui lòng nhấn vào nút bên dưới để tiếp tục:</p>

<div style='margin:24px 0;text-align:center;'>
    <a href='{resetLink}'
       style='display:inline-block;background:#111827;color:#ffffff;
              padding:14px 24px;border-radius:10px;font-size:16px;
              font-weight:bold;text-decoration:none;'>
        Đặt lại mật khẩu
    </a>
</div>

<div style='margin-top:16px;padding:14px;background:#eff6ff;border:1px solid #93c5fd;border-radius:10px;'>
    <strong>Lưu ý:</strong>
    <div style='margin-top:8px;'>
        Liên kết này chỉ có hiệu lực trong <strong>{expiredMinutes} phút</strong>.
    </div>
</div>

<p style='margin-top:16px;'>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</p>";

            return WrapLayout("Đặt lại mật khẩu SportsBicyclesStore", content);
        }

        public string BuildPasswordChangedEmail(string? fullName)
        {
            var name = string.IsNullOrWhiteSpace(fullName) ? "bạn" : fullName;

            var content = $@"
<p>Xin chào <strong>{name}</strong>,</p>
<p>Mật khẩu tài khoản <strong>SportsBicyclesStore</strong> của bạn đã được thay đổi thành công.</p>

<div style='margin-top:16px;padding:14px;background:#ecfdf5;border:1px solid #86efac;border-radius:10px;'>
    Nếu đây là thao tác của bạn, bạn có thể tiếp tục sử dụng hệ thống bình thường.
</div>

<p style='margin-top:16px;'>
    Nếu bạn <strong>không thực hiện</strong> thay đổi này, vui lòng liên hệ hỗ trợ ngay để đảm bảo an toàn tài khoản.
</p>";

            return WrapLayout("Mật khẩu tài khoản đã được thay đổi", content);
        }
    }
}

