using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.Common.Helpers;
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
    public class AdminUserService : IAdminUserService
    {
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Transaction> _transactionRepo;
        private readonly IGenericRepository<SellerShippingProfile> _shippingRepo;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _uow;

        public AdminUserService(
            IGenericRepository<User> userRepo,
            IGenericRepository<Listing> listingRepo,
            IGenericRepository<Order> orderRepo,
            IGenericRepository<Transaction> transactionRepo,
            IGenericRepository<SellerShippingProfile> shippingRepo,
            IUnitOfWork uow,
            IEmailService emailService)
        {
            _userRepo = userRepo;
            _listingRepo = listingRepo;
            _orderRepo = orderRepo;
            _transactionRepo = transactionRepo;
            _shippingRepo = shippingRepo;
            _emailService = emailService;
            _uow = uow;
        }

        private static ResponseDTO Success(object? data = null)
            => new() { IsSucess = true, BusinessCode = BusinessCode.GET_DATA_SUCCESSFULLY, Data = data };

        private static ResponseDTO Fail(string msg)
            => new() { IsSucess = false, BusinessCode = BusinessCode.EXCEPTION, Message = msg };


        public async Task<ResponseDTO> GetUsersAsync(int page, int size, string? search, string? role, bool isDesc)
        {
            try
            {
                var query = _userRepo.AsQueryable()
                    .Where(x =>
                        x.Role != RoleEnum.INSPECTOR &&
                        x.Role != RoleEnum.ADMIN);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var keyword = search.Trim().ToLower();

                    query = query.Where(x =>
                        x.FullName.ToLower().Contains(keyword) ||
                        x.Email.ToLower().Contains(keyword));
                }
                if (!string.IsNullOrWhiteSpace(role) &&
                    Enum.TryParse<RoleEnum>(role, true, out var roleEnum))
                {
                    if (roleEnum == RoleEnum.ADMIN || roleEnum == RoleEnum.INSPECTOR)
                        return Fail("Không được filter role này");

                    query = query.Where(x => x.Role == roleEnum);
                }

                query = isDesc
                    ? query.OrderByDescending(x => x.Role)
                    : query.OrderBy(x => x.Role);

                var totalItems = await query.CountAsync();

                var users = await query
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToListAsync();

                var items = users.Select(x => new AdminUserListDTO
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Email = x.Email,
                    Role = x.Role.ToString(),
                    CreatedAt = x.CreatedAt
                });

                return Success(new
                {
                    Page = page,
                    Size = size,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)size),
                    Items = items
                });
            }
            catch (Exception ex)
            {
                return Fail("Lỗi list user: " + ex.Message);
            }
        }

        // ================= DETAIL =================
        public async Task<ResponseDTO> GetUserDetailAsync(Guid userId)
        {
            try
            {
                var user = await _userRepo.GetByExpression(x => x.Id == userId);

                if (user == null)
                    return Fail("Không tìm thấy user");

                if (user.Role == RoleEnum.INSPECTOR)
                    return Fail("Không có quyền");

                // ===== COMMON =====
                var totalOrders = await _orderRepo.AsQueryable()
                    .CountAsync(x => x.UserId == userId);

                var completedOrders = await _orderRepo.AsQueryable()
                    .CountAsync(x => x.UserId == userId && x.Status == OrderStatusEnum.Completed);

                var data = new AdminUserDetailDTO
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    CreatedAt = user.CreatedAt,
                    TotalOrders = totalOrders,
                    CompletedOrders = completedOrders
                };

                // ===== SELLER =====
                if (user.Role == RoleEnum.SELLER)
                {
                    data.TotalListings = await _listingRepo.AsQueryable()
                        .CountAsync(x => x.UserId == userId && !x.IsDeleted);

                    data.TotalRevenue = await _transactionRepo.AsQueryable()
                        .Where(x => x.UserId == userId && x.Status == TransactionStatusEnum.Paid)
                        .SumAsync(x => (decimal?)x.Amount) ?? 0;

                    var shipping = await _shippingRepo.AsQueryable()
                        .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

                    data.SenderName = shipping?.SenderName;
                    data.SenderPhone = shipping?.SenderPhone;
                    data.SenderAddress = shipping?.SenderAddress;

                    data.BankName = shipping?.BankName;
                    data.BankAccountNumber = shipping?.BankAccountNumber;
                    data.BankAccountName = shipping?.BankAccountName;
                }

                // ===== BUYER =====
                if (user.Role == RoleEnum.BUYER)
                {
                    data.TotalSpent = await _transactionRepo.AsQueryable()
                        .Where(x => x.UserId == userId && x.Status == TransactionStatusEnum.Paid)
                        .SumAsync(x => (decimal?)x.Amount) ?? 0;
                }

                return Success(data);
            }
            catch (Exception ex)
            {
                return Fail("Lỗi detail user: " + ex.Message);
            }
        }

        public async Task<ResponseDTO> GetSellersAsync(int page, int size, string? search, bool isDesc)
        {
            try
            {
                var query = _userRepo.AsQueryable()
                    .Where(x => x.Role == RoleEnum.SELLER);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var keyword = search.Trim().ToLower();

                    query = query.Where(x =>
                        x.FullName.ToLower().Contains(keyword) ||
                        x.Email.ToLower().Contains(keyword));
                }

                query = isDesc
                    ? query.OrderByDescending(x => x.CreatedAt)
                    : query.OrderBy(x => x.CreatedAt);

                var totalItems = await query.CountAsync();

                var users = await query
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToListAsync();

                var items = users.Select(x => new AdminUserListDTO
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Email = x.Email,
                    Role = x.Role.ToString(),
                    CreatedAt = x.CreatedAt
                });

                return Success(new
                {
                    Page = page,
                    Size = size,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)size),
                    Items = items
                });
            }
            catch (Exception ex)
            {
                return Fail("Lỗi get sellers: " + ex.Message);
            }
        }

        public async Task<ResponseDTO> GetBuyersAsync(int page, int size, string? search, bool isDesc)
        {
            try
            {
                var query = _userRepo.AsQueryable()
                    .Where(x => x.Role == RoleEnum.BUYER);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var keyword = search.Trim().ToLower();

                    query = query.Where(x =>
                        x.FullName.ToLower().Contains(keyword) ||
                        x.Email.ToLower().Contains(keyword));
                }

                query = isDesc
                    ? query.OrderByDescending(x => x.CreatedAt)
                    : query.OrderBy(x => x.CreatedAt);

                var totalItems = await query.CountAsync();

                var users = await query
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToListAsync();

                var items = users.Select(x => new AdminUserListDTO
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Email = x.Email,
                    Role = x.Role.ToString(),
                    CreatedAt = x.CreatedAt
                });

                return Success(new
                {
                    Page = page,
                    Size = size,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)size),
                    Items = items
                });
            }
            catch (Exception ex)
            {
                return Fail("Lỗi get buyers: " + ex.Message);
            }
        }

        public async Task<ResponseDTO> BanUserAsync(Guid userId, BanUserDTO dto)
        {
            try
            {
                var user = await _userRepo.AsQueryable()
                    .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);

                if (user == null)
                    return Fail("Không tìm thấy user");

                if (user.Role == RoleEnum.ADMIN)
                    return Fail("Không thể khóa admin");

                if (user.Role == RoleEnum.INSPECTOR)
                    return Fail("Không thể khóa Inspector");

                if (user.Status == UserStatusEnum.Banned)
                    return Fail("User đã bị khóa");

                if (string.IsNullOrWhiteSpace(dto.Reason))
                    return Fail("Lý do không hợp lệ");

                // ===== UPDATE =====
                user.Status = UserStatusEnum.Banned;
                user.UpdatedAt = DateTimeHelper.NowVN();

                await _userRepo.Update(user);
                await _uow.SaveChangeAsync();

                // ===== EMAIL =====
                var subject = "[SportsBicycles] Thông báo tạm khóa tài khoản";

                var body = $@"
<div style='font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:auto;border:1px solid #eee;border-radius:8px;overflow:hidden'>
    
    <div style='background:#111;color:#fff;padding:16px;font-size:18px;font-weight:bold'>
        SportsBicycles
    </div>

    <div style='padding:20px'>
        <h2 style='color:#e74c3c;margin-top:0'>Tài khoản của bạn đã bị tạm khóa</h2>

        <p>Xin chào <b>{user.FullName}</b>,</p>

        <p>
            Chúng tôi xin thông báo rằng tài khoản của bạn trên hệ thống 
            <b>SportsBicycles</b> đã bị <b>tạm khóa</b> do phát hiện vi phạm 
            các quy định hoạt động của nền tảng.
        </p>

        <div style='background:#f8f9fa;padding:12px;border-radius:6px;border-left:4px solid #e74c3c'>
            <b>Lý do khóa tài khoản:</b>
            <p style='margin:6px 0'>{dto.Reason}</p>
        </div>

        <p style='margin-top:16px'>
            Trong thời gian bị khóa, bạn sẽ không thể:
        </p>

        <ul style='padding-left:18px'>
            <li>Đăng nhập vào hệ thống</li>
            <li>Thực hiện giao dịch mua/bán</li>
            <li>Sử dụng các tính năng liên quan</li>
        </ul>

        <p style='margin-top:16px'>
            Nếu bạn cho rằng đây là sự nhầm lẫn hoặc cần hỗ trợ, vui lòng liên hệ đội ngũ hỗ trợ của chúng tôi:
        </p>

        <p>
            📧 Email: <b>tuannhatrang.contact@gmail.com (Admin)</b><br/>
            ⏱ Thời gian phản hồi: 24 - 48 giờ làm việc
        </p>

        <p style='margin-top:20px'>
            Trân trọng,<br/>
            <b>SportsBicycles Team</b>
        </p>
    </div>

    <div style='background:#f1f1f1;padding:12px;text-align:center;font-size:12px;color:#777'>
        © {DateTime.Now.Year} SportsBicycles. All rights reserved.<br/>
        Đây là email tự động, vui lòng không trả lời email này.
    </div>
</div>";

                await _emailService.SendEmailAsync(user.Email, subject, body);

                return Success(null);
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        
        }

        public async Task<ResponseDTO> UnbanUserAsync(Guid userId)
        {
            try
            {
                var user = await _userRepo.AsQueryable()
                    .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);

                if (user == null)
                    return Fail("Không tìm thấy user");

                if (user.Status != UserStatusEnum.Banned)
                    return Fail("User không bị khóa");

                user.Status = UserStatusEnum.Active;
                user.UpdatedAt = DateTimeHelper.NowVN();

                await _userRepo.Update(user);
                await _uow.SaveChangeAsync();

                // ===== EMAIL =====
                var subject = "[SportsBicycles] Tài khoản đã được mở khóa";

                var body = $@"
<div style='font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:auto;border:1px solid #eee;border-radius:8px;overflow:hidden'>
    
    <div style='background:#111;color:#fff;padding:16px;font-size:18px;font-weight:bold'>
        SportsBicycles
    </div>

    <div style='padding:20px'>
        <h2 style='color:#27ae60;margin-top:0'>Tài khoản của bạn đã được mở khóa</h2>

        <p>Xin chào <b>{user.FullName}</b>,</p>

        <p>
            Chúng tôi xin thông báo rằng tài khoản của bạn trên hệ thống 
            <b>SportsBicycles</b> đã được <b>khôi phục</b> và có thể sử dụng bình thường.
        </p>

        <p>
            Bạn có thể tiếp tục:
        </p>

        <ul style='padding-left:18px'>
            <li>Đăng nhập và sử dụng hệ thống</li>
            <li>Thực hiện giao dịch mua/bán</li>
            <li>Quản lý tài khoản và đơn hàng</li>
        </ul>

        <div style='background:#f8f9fa;padding:12px;border-radius:6px;border-left:4px solid #27ae60;margin-top:12px'>
            <b>Lưu ý:</b>
            <p style='margin:6px 0'>
                Vui lòng tuân thủ các quy định của nền tảng để tránh việc tài khoản bị hạn chế trong tương lai.
            </p>
        </div>

        <p style='margin-top:20px'>
            Nếu bạn cần hỗ trợ thêm, vui lòng liên hệ:
        </p>

        <p>
            📧 Email: <b>tuannhatrang.contact@gmail.com (Admin)</b>
        </p>

        <p style='margin-top:20px'>
            Trân trọng,<br/>
            <b>SportsBicycles Team</b>
        </p>
    </div>

    <div style='background:#f1f1f1;padding:12px;text-align:center;font-size:12px;color:#777'>
        © {DateTime.Now.Year} SportsBicycles. All rights reserved.<br/>
        Đây là email tự động, vui lòng không trả lời email này.
    </div>
</div>";

                await _emailService.SendEmailAsync(user.Email, subject, body);

                return Success(null);
            }
            catch (Exception ex)
            {
                return Fail( ex.Message);
            }
        }
    }
}
