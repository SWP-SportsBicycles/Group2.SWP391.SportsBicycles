using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class AdminListingService : IAdminListingService
    {
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _uow;

        public AdminListingService(
            IGenericRepository<Listing> listingRepo,
            IGenericRepository<Bike> bikeRepo,
           IGenericRepository<User> userRepo,
            IEmailService emailService,
            IUnitOfWork uow)
        {
            _listingRepo = listingRepo;
            _bikeRepo = bikeRepo;
            _userRepo = userRepo;
            _emailService = emailService;
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

        // ================= APPROVE =================
        public async Task<ResponseDTO> ApproveListingAsync(Guid listingId)
        {
            ResponseDTO dto = new ResponseDTO();

            try
            {
                var listing = await _listingRepo.AsQueryable()
                    .Include(l => l.Bikes)
                    .ThenInclude(b => b.Medias)
                    .FirstOrDefaultAsync(l => l.Id == listingId);

                if (listing == null)
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.DATA_NOT_FOUND;
                    dto.Message = "Không tìm thấy listing.";
                    return dto;
                }

                if (listing.Status != ListingStatusEnum.PendingInspection)
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.INVALID_ACTION;
                    dto.Message = "Listing không ở trạng thái chờ duyệt.";
                    return dto;
                }

                var bike = listing.Bikes.FirstOrDefault();
                if (bike == null)
                {
                    return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy bike.");
                }

                if (bike.Status != BikeStatusEnum.PendingInspection)
                {
                    return Fail(BusinessCode.INVALID_ACTION, "Bike chưa sẵn sàng duyệt");
                }

                // ===== MEDIA VALID =====
                var medias = bike.Medias?.Where(m => !m.IsDeleted).ToList();

                if (medias == null || !medias.Any())
                {
                    return Fail(BusinessCode.INVALID_DATA, "Listing chưa có media");
                }

                var hasImage = medias.Any(m => m.Image != null);
                if (!hasImage)
                {
                    return Fail(BusinessCode.INVALID_DATA, "Phải có ít nhất 1 ảnh");
                }

                // ================= 🔥 COMMISSION 5% =================
                const decimal COMMISSION = 0.05m;

                // chỉ apply 1 lần duy nhất ở PendingReview
                bike.Price = Math.Round(bike.Price * (1 + COMMISSION), 0);

                // ================= APPROVE =================
                listing.Status = ListingStatusEnum.     Published;
                bike.Status = BikeStatusEnum.Available;

                await _uow.SaveChangeAsync();

                // ================= EMAIL =================
                var seller = await _userRepo.GetById(listing.UserId);

                if (!string.IsNullOrEmpty(seller?.Email))
                {
                    string subject = "🎉 Listing đã được duyệt";

                    string body = $@"
<html>
<body style='font-family: Arial; background:#f4f6f8; padding:20px;'>
<div style='max-width:600px; margin:auto; background:white; border-radius:10px;'>

    <div style='background:#28a745; color:white; padding:15px;'>
        Listing Approved
    </div>

    <div style='padding:20px'>
        <p>Xin chào <strong>{seller.FullName}</strong>,</p>

        <p>Bài đăng của bạn:</p>

        <div style='background:#f8f9fa; padding:15px; border-radius:8px'>
            <strong>{listing.Title}</strong>
        </div>

        <p style='color:#28a745'>
            Đã được duyệt thành công 🎉
        </p>

        <p>Giá bán trên sàn: <strong>{bike.Price:N0} VNĐ</strong></p>

        <p>Chúc bạn bán hàng thuận lợi 🚴</p>
    </div>

</div>
</body>
</html>";

                    await _emailService.SendEmailAsync(seller.Email, subject, body);
                }

                dto.IsSucess = true;
                dto.BusinessCode = BusinessCode.UPDATE_SUCESSFULLY;
                dto.Message = "Duyệt listing thành công.";
            }
            catch (Exception ex)
            {
                dto.IsSucess = false;
                dto.BusinessCode = BusinessCode.EXCEPTION;
                dto.Message = "Lỗi khi duyệt listing: " + ex.Message;
            }

            return dto;
        }

        // ================= REJECT =================
        public async Task<ResponseDTO> RejectListingAsync(Guid listingId, RejectListingDTO rejectDto)
        {
            ResponseDTO dto = new ResponseDTO();

            try
            {
                var listing = await _listingRepo.GetByExpression(l => l.Id == listingId);

                if (listing == null)
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.DATA_NOT_FOUND;
                    dto.Message = "Không tìm thấy listing.";
                    return dto;
                }

                if (listing.Status != ListingStatusEnum.PendingInspection)
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.INVALID_ACTION;
                    dto.Message = "Listing không ở trạng thái chờ duyệt.";
                    return dto;
                }
                if (string.IsNullOrWhiteSpace(rejectDto?.Reason))
                {
                    return Fail(BusinessCode.INVALID_DATA, "Phải nhập lý do reject");
                }
                listing.Status = ListingStatusEnum.Rejected;
                listing.RejectReason = rejectDto.Reason;

                await _uow.SaveChangeAsync();

                // 📩 EMAIL
                var seller = await _userRepo.GetById(listing.UserId);

                if (!string.IsNullOrEmpty(seller?.Email))
                {
                    string subject = "❌ Listing bị từ chối";

                    string body = $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f4f6f8; padding:20px;'>
    <div style='max-width:600px; margin:auto; background:white; border-radius:10px; overflow:hidden; box-shadow:0 4px 10px rgba(0,0,0,0.05);'>
        
        <div style='background:#dc3545; color:white; padding:15px 20px; font-size:18px; font-weight:bold;'>
            ❌ Listing Rejected
        </div>

        <div style='padding:20px; color:#333;'>
            <p>Xin chào <strong>{seller.FullName}</strong>,</p>

            <p>Bài đăng của bạn:</p>

            <div style='background:#f8f9fa; padding:15px; border-radius:8px; margin:15px 0;'>
                <strong>{listing.Title}</strong>
            </div>

            <p>đã bị <strong style='color:#dc3545;'>từ chối</strong>.</p>

            <p><strong>Lý do:</strong></p>

            <div style='background:#fff3cd; padding:15px; border-radius:8px; margin:10px 0; color:#856404;'>
                {rejectDto.Reason}
            </div>

            <p>Vui lòng chỉnh sửa và gửi lại để được duyệt.</p>

            <div style='margin-top:30px; font-size:14px; color:#888;'>
                Trân trọng,<br/>
                <strong>SportsBicycles Team</strong>
            </div>
        </div>

        <div style='background:#f1f1f1; padding:10px; text-align:center; font-size:12px; color:#888;'>
            © 2026 SportsBicycles. All rights reserved.
        </div>
    </div>
</body>
</html>";

                    await _emailService.SendEmailAsync(seller.Email, subject, body);
                }

                dto.IsSucess = true;
                dto.BusinessCode = BusinessCode.UPDATE_SUCESSFULLY;
                dto.Message = "Từ chối listing thành công.";
            }
            catch (Exception ex)
            {
                dto.IsSucess = false;
                dto.BusinessCode = BusinessCode.EXCEPTION;
                dto.Message = "Lỗi khi reject listing: " + ex.Message;
            }

            return dto;
        }

        public async Task<ResponseDTO> GetListingsAsync(int page, int size, string? search, string? sortBy, bool isDesc)
        {
            try
            {
                var query = _listingRepo.AsQueryable()
                    .Include(l => l.Bikes)
                        .ThenInclude(b => b.Medias)
                    .Where(l => l.Status == ListingStatusEnum.PendingInspection);

                // ===== SEARCH =====
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var keyword = search.Trim().ToLower();

                    query = query.Where(l =>
                        l.Title.ToLower().Contains(keyword) ||
                        l.Bikes.Any(b => b.Brand.ToLower().Contains(keyword)));
                }

                // ===== SORT =====
                query = sortBy?.ToLower() switch
                {
                    "price" => isDesc
                        ? query.OrderByDescending(l => l.Bikes.FirstOrDefault().Price)
                        : query.OrderBy(l => l.Bikes.FirstOrDefault().Price),

                    _ => isDesc
                        ? query.OrderByDescending(l => l.CreatedAt)
                        : query.OrderBy(l => l.CreatedAt)
                };

                var totalItems = await query.CountAsync();

                var listings = await query
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToListAsync();

                // ===== MAP DTO =====
                var items = listings.Select(l =>
                {
                    var bike = l.Bikes.FirstOrDefault();
                    var medias = bike?.Medias?.Where(m => !m.IsDeleted).ToList();

                    return new AdminListingDTO
                    {
                        Id = l.Id,
                        Title = l.Title,
                        City = l.City,

                        Price = bike?.Price ?? 0,
                        Brand = bike?.Brand ?? "",

                        Status = l.Status.ToString(),

                        Thumbnail = medias?.FirstOrDefault(m => m.Image != null)?.Image,
                        TotalImages = medias?.Count(m => m.Image != null) ?? 0,
                        HasVideo = medias?.Any(m => m.VideoUrl != null) ?? false
                    };
                }).ToList();

                return Success(new
                {
                    PageNumber = page,
                    PageSize = size,
                    TotalItems = totalItems,
                    Items = items
                });
            }
            catch (Exception ex)
            {
                return Fail(BusinessCode.EXCEPTION, "Lỗi khi lấy listing: " + ex.Message);
            }
        }

        public async Task<ResponseDTO> GetDetailAsync(Guid listingId)
        {
            try
            {
                var listing = await _listingRepo.AsQueryable()
                    .Include(l => l.Bikes)
                        .ThenInclude(b => b.Medias)
                    .FirstOrDefaultAsync(l => l.Id == listingId);

                if (listing == null)
                    return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy listing");

                var bike = listing.Bikes.FirstOrDefault();
                var medias = bike?.Medias?.Where(m => !m.IsDeleted).ToList();

                var data = new
                {
                    listing.Id,
                    listing.Title,
                    listing.Description,
                    Status = listing.Status.ToString(),
                    listing.City,

                    Bike = bike == null ? null : new
                    {
                        bike.Brand,
                        bike.Category,
                        bike.FrameSize,
                        bike.Price
                    },



                    Medias = medias?.Select(m => new
                    {
                        m.Id,
                        m.Image,
                        m.VideoUrl,
                        m.Type
                    })
                };

                return Success(data);
            }
            catch (Exception ex)
            {
                return Fail(BusinessCode.EXCEPTION, ex.Message);
            }
        }
    }
}