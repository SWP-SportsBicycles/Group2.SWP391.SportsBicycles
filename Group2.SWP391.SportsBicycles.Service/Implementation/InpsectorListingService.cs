using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class InspectorListingService : IInspectorListingService
    {
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IGenericRepository<Inspection> _inspectionRepo;
        private readonly IUnitOfWork _uow;

        public InspectorListingService(
            IGenericRepository<Listing> listingRepo,
            IGenericRepository<Inspection> inspectionRepo,
            IUnitOfWork uow)
        {
            _listingRepo = listingRepo;
            _inspectionRepo = inspectionRepo;
            _uow = uow;
        }

        private static ResponseDTO Success(object? data = null,
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

        private static bool IsValidVideoUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false; // video bắt buộc

            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static List<string> ValidateListing(Listing listing)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(listing.Title))
                errors.Add("Thiếu tiêu đề listing");

            if (string.IsNullOrWhiteSpace(listing.Description))
                errors.Add("Thiếu mô tả listing");

            if (string.IsNullOrWhiteSpace(listing.City))
                errors.Add("Thiếu city");

            if (listing.Bikes == null || !listing.Bikes.Any())
            {
                errors.Add("Listing chưa có bike");
                return errors;
            }

            foreach (var bike in listing.Bikes)
            {
                if (string.IsNullOrWhiteSpace(bike.SerialNumber))
                    errors.Add($"Bike {bike.Id}: thiếu serial number");

                if (string.IsNullOrWhiteSpace(bike.Brand))
                    errors.Add($"Bike {bike.Id}: thiếu brand");

                if (string.IsNullOrWhiteSpace(bike.Category))
                    errors.Add($"Bike {bike.Id}: thiếu category");

                if (string.IsNullOrWhiteSpace(bike.FrameSize))
                    errors.Add($"Bike {bike.Id}: thiếu frame size");

                if (string.IsNullOrWhiteSpace(bike.FrameMaterial))
                    errors.Add($"Bike {bike.Id}: thiếu frame material");

                if (string.IsNullOrWhiteSpace(bike.Condition))
                    errors.Add($"Bike {bike.Id}: thiếu condition");

                if (bike.Price <= 0)
                    errors.Add($"Bike {bike.Id}: giá phải lớn hơn 0");

                if (bike.Medias == null || !bike.Medias.Any())
                {
                    errors.Add($"Bike {bike.Id}: chưa có media");
                    continue;
                }

                var hasImage = bike.Medias.Any(x => !string.IsNullOrWhiteSpace(x.Image));
                if (!hasImage)
                    errors.Add($"Bike {bike.Id}: phải có ít nhất 1 ảnh");

                var hasVideo = bike.Medias.Any(x => !string.IsNullOrWhiteSpace(x.VideoUrl));
                if (!hasVideo)
                    errors.Add($"Bike {bike.Id}: bắt buộc phải có video");

                foreach (var media in bike.Medias)
                {
                    if (!string.IsNullOrWhiteSpace(media.VideoUrl) && !IsValidVideoUrl(media.VideoUrl))
                        errors.Add($"Bike {bike.Id}: video url không hợp lệ");
                }
            }

            return errors;
        }

        public async Task<ResponseDTO> GetPendingListingsAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0 || pageSize <= 0)
                return Fail(BusinessCode.INVALID_INPUT, "Invalid pagination");

            var query = _listingRepo.AsQueryable()
                .Include(l => l.User)
                .Include(l => l.Bikes)
                    .ThenInclude(b => b.Medias)
                .Where(l => l.Status == ListingStatusEnum.PendingInspection)
                .OrderByDescending(l => l.CreatedAt);

            var totalItems = await query.CountAsync();

            var listings = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = listings.Select(l =>
            {
                var firstBike = l.Bikes.FirstOrDefault();
                var medias = firstBike?.Medias ?? new List<Media>();

                return new
                {
                    ListingId = l.Id,
                    l.Title,
                    l.City,
                    SellerName = l.User.FullName,
                    Price = firstBike?.Price ?? 0,
                    Brand = firstBike?.Brand,
                    Category = firstBike?.Category,
                    Thumbnail = medias.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.Image))?.Image,
                    VideoUrl = medias.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.VideoUrl))?.VideoUrl,
                    CreatedAt = l.CreatedAt
                };
            }).ToList();

            return Success(new
            {
                Items = items,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        public async Task<ResponseDTO> GetListingDetailAsync(Guid listingId)
        {
            if (listingId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "ListingId không hợp lệ");

            var listing = await _listingRepo.AsQueryable()
                .Include(l => l.User)
                .Include(l => l.Bikes)
                    .ThenInclude(b => b.Medias)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy listing");

            return Success(new
            {
                ListingId = listing.Id,
                listing.Title,
                listing.Description,
                listing.City,
                Status = listing.Status.ToString(),
                listing.RejectReason,
                Seller = new
                {
                    listing.UserId,
                    listing.User.FullName,
                    listing.User.Email,
                    listing.User.PhoneNumber
                },
                Bikes = listing.Bikes.Select(b => new
                {
                    BikeId = b.Id,
                    b.SerialNumber,
                    b.Brand,
                    b.Category,
                    b.FrameSize,
                    b.FrameMaterial,
                    b.Condition,
                    b.Paint,
                    b.Groupset,
                    b.Operating,
                    b.TireRim,
                    b.BrakeType,
                    b.Overall,
                    b.Price,
                    b.City,
                    Medias = b.Medias.Select(m => new
                    {
                        m.Id,
                        m.Image,
                        m.VideoUrl,
                        Type = m.Type.ToString()
                    }).ToList()
                }).ToList()
            });
        }

        public async Task<ResponseDTO> SubmitToAdminAsync(Guid inspectorId, Guid listingId, ReviewListingDTO dto)
        {
            if (inspectorId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được inspector");

            if (listingId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "ListingId không hợp lệ");

            if (dto == null)
                return Fail(BusinessCode.INVALID_INPUT, "Dữ liệu không hợp lệ");

            await _uow.BeginTransactionAsync();

            try
            {
                var listing = await _listingRepo.AsQueryable()
                    .Include(l => l.Bikes)
                        .ThenInclude(b => b.Medias)
                    .Include(l => l.Bikes)
                        .ThenInclude(b => b.Inspection)
                    .FirstOrDefaultAsync(l => l.Id == listingId);

                if (listing == null)
                    return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy listing");

                if (listing.Status != ListingStatusEnum.PendingInspection)
                    return Fail(BusinessCode.INVALID_ACTION, "Listing không ở trạng thái chờ inspector kiểm tra");

                var validationErrors = ValidateListing(listing);
                var finalComment = validationErrors.Any()
                    ? string.Join(" | ", validationErrors)
                    : (string.IsNullOrWhiteSpace(dto.Comment) ? "Inspector checked and sent to admin" : dto.Comment.Trim());

                foreach (var bike in listing.Bikes)
                {
                    var inspection = new Inspection
                    {
                        Id = Guid.NewGuid(),
                        UserId = inspectorId,
                        Frame = !validationErrors.Any(),
                        PaintCondition = !validationErrors.Any(),
                        Drivetrain = !validationErrors.Any(),
                        Brakes = !validationErrors.Any(),
                        Score = validationErrors.Any() ? 0 : 10,
                        Comment = finalComment,
                        InspectionDate = DateTime.UtcNow
                    };

                    await _inspectionRepo.Insert(inspection);

                    bike.InspectionId = inspection.Id;
                    bike.Overall = validationErrors.Any() ? "Need Admin Review - Invalid" : "Checked";
                }

                // Không đổi sang Published ở đây
                // Không reject cuối ở đây
                // Vẫn giữ PendingInspection để admin xử lý tiếp

                await _uow.SaveChangeAsync();
                await _uow.CommitAsync();

                return Success(new
                {
                    ListingId = listing.Id,
                    Status = listing.Status.ToString(),
                    Comment = finalComment,
                    SentToAdmin = true
                }, BusinessCode.UPDATE_SUCESSFULLY);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
    }
}