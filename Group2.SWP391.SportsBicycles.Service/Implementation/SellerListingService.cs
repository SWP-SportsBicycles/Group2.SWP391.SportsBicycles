using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class SellerListingService : ISellerListingService
    {
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IUnitOfWork _uow;
        private readonly IGenericRepository<SellerShippingProfile> _shippingRepo;

        public SellerListingService(
            IGenericRepository<Listing> listingRepo,
            IGenericRepository<Bike> bikeRepo,
            IGenericRepository<Order> orderRepo,
            IGenericRepository<SellerShippingProfile> shippingRepo,
            IUnitOfWork uow)
        {
            _listingRepo = listingRepo;
            _bikeRepo = bikeRepo;
            _orderRepo = orderRepo;
            _shippingRepo = shippingRepo;
            _uow = uow;
        }

        // ================= HELPER =================
        private static readonly string[] ValidCities = new[] { "Hà Nội", "TP.HCM", "Đà Nẵng" };

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

        

        private static bool IsValidCity(string? city)
        {
            return !string.IsNullOrWhiteSpace(city) && ValidCities.Contains(city.Trim());
        }

        private async Task<bool> HasActiveOrderAsync(Guid listingId)
        {
            return await _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                .AnyAsync(o =>
                    (
                        o.Status == OrderStatusEnum.Pending ||
                        o.Status == OrderStatusEnum.Paid ||
                        o.Status == OrderStatusEnum.Confirmed ||
                        o.Status == OrderStatusEnum.Shipping
                    )
                    &&
                    o.OrderItems.Any(oi =>
                        oi.Bike != null &&
                        oi.Bike.ListingId == listingId
                    )
                );
        }

        private async Task<Bike?> GetBikeByListingIdAsync(Guid listingId)
        {
            return await _bikeRepo.AsQueryable()
    .Include(b => b.Medias.Where(m => !m.IsDeleted))
    .FirstOrDefaultAsync(b => b.ListingId == listingId && !b.IsDeleted);
        }

       

        private ResponseDTO ValidateCreateOrUpdateDto(ListingCreateDTO dto)
        {
            if (dto == null)
                return Fail(BusinessCode.INVALID_DATA, "Dữ liệu không hợp lệ");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu title");

            if (string.IsNullOrWhiteSpace(dto.Description))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu description");

            if (string.IsNullOrWhiteSpace(dto.SerialNumber))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu serial number");

            if (string.IsNullOrWhiteSpace(dto.Brand))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu brand");

            if (string.IsNullOrWhiteSpace(dto.Category))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu category");

            if (string.IsNullOrWhiteSpace(dto.FrameSize))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu frame size");

            if (string.IsNullOrWhiteSpace(dto.FrameMaterial))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu frame material");

            if (string.IsNullOrWhiteSpace(dto.Condition))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu condition");

            if (string.IsNullOrWhiteSpace(dto.Paint))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu paint");

            if (string.IsNullOrWhiteSpace(dto.Groupset))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu groupset");

            if (string.IsNullOrWhiteSpace(dto.Operating))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu operating");

            if (string.IsNullOrWhiteSpace(dto.TireRim))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu tire/rim");

            if (string.IsNullOrWhiteSpace(dto.BrakeType))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu brake type");

            if (string.IsNullOrWhiteSpace(dto.Overall))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu overall");

            if (!IsValidCity(dto.City))
                return Fail(BusinessCode.INVALID_DATA, "City không hợp lệ");

            if (dto.Price <= 0)
                return Fail(BusinessCode.INVALID_DATA, "Price không hợp lệ");

            return Success();
        }

        private ResponseDTO ValidateUpdateDto(ListingUpsertDTO dto)
        {
            if (dto == null)
                return Fail(BusinessCode.INVALID_DATA, "Dữ liệu không hợp lệ");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu title");

            if (string.IsNullOrWhiteSpace(dto.Description))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu description");

            if (string.IsNullOrWhiteSpace(dto.SerialNumber))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu serial number");

            if (string.IsNullOrWhiteSpace(dto.Brand))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu brand");

            if (string.IsNullOrWhiteSpace(dto.Category))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu category");

            if (string.IsNullOrWhiteSpace(dto.FrameSize))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu frame size");

            if (string.IsNullOrWhiteSpace(dto.FrameMaterial))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu frame material");

            if (string.IsNullOrWhiteSpace(dto.Condition))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu condition");

            if (string.IsNullOrWhiteSpace(dto.Paint))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu paint");

            if (string.IsNullOrWhiteSpace(dto.Groupset))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu groupset");

            if (string.IsNullOrWhiteSpace(dto.Operating))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu operating");

            if (string.IsNullOrWhiteSpace(dto.TireRim))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu tire/rim");

            if (string.IsNullOrWhiteSpace(dto.BrakeType))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu brake type");

            if (string.IsNullOrWhiteSpace(dto.Overall))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu overall");

            if (!IsValidCity(dto.City))
                return Fail(BusinessCode.INVALID_DATA, "City không hợp lệ");

            if (dto.Price <= 0)
                return Fail(BusinessCode.INVALID_DATA, "Price không hợp lệ");

            return Success();
        }

        // ================= CREATE =================
        public async Task<ResponseDTO> CreateAsync(Guid sellerId, ListingCreateDTO dto)
        {
            // ===== 🔥 CHECK SHIPPING PROFILE =====
            var hasProfile = await _shippingRepo.AsQueryable()
                .AnyAsync(x => x.UserId == sellerId && !x.IsDeleted);

            if (!hasProfile)
                return Fail(BusinessCode.INVALID_ACTION, "Vui lòng cập nhật địa chỉ giao hàng trước khi đăng bài");


            if (dto == null)
                return Fail(BusinessCode.INVALID_INPUT, "Data null");

            if (string.IsNullOrWhiteSpace(dto.SerialNumber))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu serial number");

            var validate = ValidateCreateOrUpdateDto(dto);
            if (!validate.IsSucess)
                return validate;

            var serialNumber = dto.SerialNumber.Trim();

            // ❌ duplicate global
            if (await _bikeRepo.AsQueryable().AnyAsync(b => b.SerialNumber == serialNumber))
                return Fail(BusinessCode.DUPLICATE_DATA, "Serial number đã tồn tại");

            // ❌ anti-spam cùng seller (trừ rejected)
            var isSpam = await _listingRepo.AsQueryable()
                .Include(l => l.Bikes)
                .AnyAsync(l =>
                    l.UserId == sellerId &&
                    l.Bikes.Any(b => b.SerialNumber == serialNumber) &&
                    l.Status != ListingStatusEnum.Rejected);

            if (isSpam)
                return Fail(BusinessCode.DUPLICATE_DATA, "Bạn đã đăng xe này rồi");

            // ===== CREATE LISTING =====
            var listing = new Listing
            {
                Id = Guid.NewGuid(),
                UserId = sellerId,
                Title = dto.Title?.Trim() ?? string.Empty,
                Description = dto.Description?.Trim() ?? string.Empty,
                City = dto.City?.Trim() ?? string.Empty,
                Status = ListingStatusEnum.Draft,
                CreatedAt = DateTime.UtcNow
            };

            await _listingRepo.Insert(listing);

            // ===== CREATE BIKE =====
            var bike = new Bike
            {
                Id = Guid.NewGuid(),
                ListingId = listing.Id,
                SerialNumber = serialNumber,
                Category = dto.Category?.Trim() ?? string.Empty,
                Brand = dto.Brand?.Trim() ?? string.Empty,
                FrameSize = dto.FrameSize?.Trim() ?? string.Empty,
                FrameMaterial = dto.FrameMaterial?.Trim() ?? string.Empty,
                Condition = dto.Condition?.Trim() ?? string.Empty,
                Paint = dto.Paint?.Trim() ?? string.Empty,
                Groupset = dto.Groupset?.Trim() ?? string.Empty,
                Operating = dto.Operating?.Trim() ?? string.Empty,
                TireRim = dto.TireRim?.Trim() ?? string.Empty,
                BrakeType = dto.BrakeType?.Trim() ?? string.Empty,
                Overall = dto.Overall?.Trim() ?? string.Empty,
                OriginalPrice = dto.Price,
                SalePrice = dto.Price,
                Price = dto.Price,
                City = dto.City?.Trim() ?? string.Empty,
                Status = BikeStatusEnum.PendingInspection
            };

            await _bikeRepo.Insert(bike);

            await _uow.SaveChangeAsync();

            return Success(new { listingId = listing.Id }, BusinessCode.CREATED_SUCCESSFULLY);
        }

        // ================= SUBMIT =================
        public async Task<ResponseDTO> SubmitForReviewAsync(Guid sellerId, Guid listingId)
        {
            var listing = await _listingRepo.GetByExpression(
        x => x.Id == listingId && x.UserId == sellerId);

            if (listing == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy listing");

            if (listing.Status != ListingStatusEnum.Draft)
                return Fail(BusinessCode.INVALID_ACTION, "Chỉ draft mới được submit");

            var bike = await _bikeRepo.AsQueryable()
                .Include(b => b.Medias)
                .FirstOrDefaultAsync(b => b.ListingId == listingId && !b.IsDeleted);

            if (bike == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy bike");

            // 🔥 filter media hợp lệ
            var medias = bike.Medias?
                .Where(m => !m.IsDeleted)
                .ToList();

            if (medias == null || !medias.Any())
                return Fail(BusinessCode.INVALID_DATA, "Phải upload ít nhất 1 ảnh hoặc video");

            // ✅ phải có ít nhất 1 ảnh
            if (!medias.Any(m => m.Image != null))
                return Fail(BusinessCode.INVALID_DATA, "Phải có ít nhất 1 ảnh");

            // OPTIONAL: bắt video
            // if (!medias.Any(m => m.VideoUrl != null))
            //     return Fail(BusinessCode.INVALID_DATA, "Phải có video");

            // ===== CHECK FULL DATA =====
            if (string.IsNullOrWhiteSpace(listing.Title) ||
                string.IsNullOrWhiteSpace(listing.Description) ||
                string.IsNullOrWhiteSpace(bike.SerialNumber) ||
                string.IsNullOrWhiteSpace(bike.Brand) ||
                string.IsNullOrWhiteSpace(bike.Category) ||
                string.IsNullOrWhiteSpace(bike.FrameSize) ||
                string.IsNullOrWhiteSpace(bike.FrameMaterial) ||
                string.IsNullOrWhiteSpace(bike.Condition) ||
                string.IsNullOrWhiteSpace(bike.Paint) ||
                string.IsNullOrWhiteSpace(bike.Groupset) ||
                string.IsNullOrWhiteSpace(bike.Operating) ||
                string.IsNullOrWhiteSpace(bike.TireRim) ||
                string.IsNullOrWhiteSpace(bike.BrakeType) ||
                string.IsNullOrWhiteSpace(bike.Overall) ||
                !IsValidCity(bike.City) ||
                bike.Price <= 0)
            {
                return Fail(BusinessCode.INVALID_DATA, "Listing chưa đủ dữ liệu để submit");
            }

            // ===== SUBMIT =====
            listing.Status = ListingStatusEnum.PendingInspection;
            bike.Status = BikeStatusEnum.PendingInspection;

            await _uow.SaveChangeAsync();

            return Success(null, BusinessCode.UPDATE_SUCESSFULLY);
        }

        // ================= UPDATE =================
        public async Task<ResponseDTO> UpdateAsync(Guid sellerId, Guid listingId, ListingUpsertDTO dto)
        {
            var listing = await _listingRepo.GetByExpression(
        x => x.Id == listingId && x.UserId == sellerId);

            if (listing == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy listing");

            var bike = await GetBikeByListingIdAsync(listingId);
            if (bike == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy bike");

            // ❌ có đơn thì cấm
            if (await HasActiveOrderAsync(listingId))
                return Fail(BusinessCode.INVALID_ACTION, "Đang có đơn active");

            // ❌ withdraw thì cấm
            if (listing.Status == ListingStatusEnum.Withdrawn)
                return Fail(BusinessCode.INVALID_ACTION, "Listing đã withdraw");

            // ================= 🔥 RULE 1: 3 TIẾNG =================
            var hours = (DateTime.UtcNow - listing.CreatedAt).TotalHours;
            if (hours > 3)
                return Fail(BusinessCode.INVALID_ACTION, "Chỉ được edit trong 3 giờ đầu");

            // ================= 🔥 RULE 2: LOCK SAU INSPECT =================
            if (listing.Status == ListingStatusEnum.PendingInspection ||
                listing.Status == ListingStatusEnum.PendingReview ||
                listing.Status == ListingStatusEnum.Published)
            {
                return Fail(BusinessCode.INVALID_ACTION, "Listing đã qua kiểm duyệt, không được sửa");
            }

            var validate = ValidateUpdateDto(dto);
            if (!validate.IsSucess)
                return validate;

            // ===== UPDATE =====
            listing.Title = dto.Title.Trim();
            listing.Description = dto.Description.Trim();
            listing.UpdatedAt = DateTime.UtcNow;

            bike.SerialNumber = dto.SerialNumber.Trim();
            bike.Category = dto.Category.Trim();
            bike.Brand = dto.Brand.Trim();
            bike.FrameSize = dto.FrameSize.Trim();
            bike.FrameMaterial = dto.FrameMaterial.Trim();
            bike.Condition = dto.Condition.Trim();
            bike.Paint = dto.Paint.Trim();
            bike.Groupset = dto.Groupset.Trim();
            bike.Operating = dto.Operating.Trim();
            bike.TireRim = dto.TireRim.Trim();
            bike.BrakeType = dto.BrakeType.Trim();
            bike.Overall = dto.Overall.Trim();
            bike.OriginalPrice = dto.Price;
            bike.SalePrice = dto.Price; // reset lại
            bike.City = dto.City.Trim();

            await _uow.SaveChangeAsync();

            return Success(null, BusinessCode.UPDATE_SUCESSFULLY);
        }

        // ================= DELETE =================
        public async Task<ResponseDTO> DeleteAsync(Guid sellerId, Guid listingId)
        {
            var listing = await _listingRepo.GetByExpression(
                x => x.Id == listingId && x.UserId == sellerId);

            if (listing == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy listing");

            if (await HasActiveOrderAsync(listingId))
                return Fail(BusinessCode.INVALID_ACTION, "Đang có đơn active, không thể xóa");

            var bike = await GetBikeByListingIdAsync(listingId);

            // ✅ soft delete
            listing.IsDeleted = true;

            if (bike != null)
            {
                bike.IsDeleted = true;

              
            }

            await _uow.SaveChangeAsync();

            return Success(null, BusinessCode.DELETE_SUCESSFULLY);
        }

        // ================= GET MY LISTINGS =================
        public async Task<ResponseDTO> GetMyListingsAsync(Guid sellerId, int pageNumber, int pageSize)
        {
            if (pageNumber <= 0 || pageSize <= 0)
                return Fail(BusinessCode.INVALID_INPUT, "Invalid pagination");

            var query = _listingRepo.AsQueryable()
                .Include(l => l.Bikes)
                    .ThenInclude(b => b.Medias)
                .Where(l => l.UserId == sellerId)
                .OrderByDescending(l => l.CreatedAt);

            var totalItems = await query.CountAsync();

            var listings = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // sync reserved
            foreach (var l in listings)
            {
                if (await HasActiveOrderAsync(l.Id))
                    l.Status = ListingStatusEnum.Reserved;
            }

            var items = listings.Select(l =>
            {
                var bike = l.Bikes.FirstOrDefault();
                var thumbnail = bike?.Medias?.FirstOrDefault(m => !m.IsDeleted)?.Image;

                return new ListingDTO
                {
                    Id = l.Id,
                    Title = l.Title,
                    Description = l.Description,
                    Status = l.Status.ToString(),
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt,
                    City = bike?.City ?? string.Empty,
                    SerialNumber = bike?.SerialNumber ?? string.Empty,
                    Price = bike?.Price ?? 0,
                    Brand = bike?.Brand ?? string.Empty,
                    Category = bike?.Category ?? string.Empty,
                    FrameSize = bike?.FrameSize ?? string.Empty,
                    Thumbnail = thumbnail
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

        // ================= DETAILS =================
        public async Task<ResponseDTO> GetDetailsAsync(Guid sellerId, Guid listingId)
        {
            var listing = await _listingRepo.AsQueryable()
               .Include(l => l.Bikes)
                   .ThenInclude(b => b.Medias)
               .FirstOrDefaultAsync(l => l.Id == listingId && l.UserId == sellerId);

            if (listing == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy listing");

            var bike = listing.Bikes.FirstOrDefault();
            if (bike == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy bike");

            var dto = new ListingDetailsDTO
            {
                Id = listing.Id,
                Title = listing.Title,
                Description = listing.Description,
                Status = listing.Status.ToString(),
                CreatedAt = listing.CreatedAt,
                UpdatedAt = listing.UpdatedAt,
                Bike = new BikeDetailDTO
                {
                    BikeId = bike.Id,
                    SerialNumber = bike.SerialNumber,
                    Category = bike.Category,
                    Brand = bike.Brand,
                    FrameSize = bike.FrameSize,
                    FrameMaterial = bike.FrameMaterial,
                    Condition = bike.Condition,
                    Paint = bike.Paint,
                    Groupset = bike.Groupset,
                    Operating = bike.Operating,
                    TireRim = bike.TireRim,
                    BrakeType = bike.BrakeType,
                    Overall = bike.Overall,
                    Price = bike.Price,
                    City = bike.City,
                    Status = bike.Status.ToString()
                },
                Medias = bike.Medias.Select(m => new MediaDTO
                {
                    Image = m.Image,
                    VideoUrl = m.VideoUrl,
                    Type = m.Type
                }).ToList()
            };

            return Success(dto);
        }

        // ================= WITHDRAW =================
        public async Task<ResponseDTO> WithdrawAsync(Guid sellerId, Guid listingId)
        {
            var listing = await _listingRepo.GetByExpression(
                x => x.Id == listingId && x.UserId == sellerId);

            if (listing == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy listing");

            var hasActiveOrder = await HasActiveOrderAsync(listingId);
            if (hasActiveOrder)
                return Fail(BusinessCode.INVALID_ACTION, "Đang có đơn active");

            if (listing.Status == ListingStatusEnum.Withdrawn)
                return Fail(BusinessCode.INVALID_ACTION, "Listing đã được withdraw");

            listing.Status = ListingStatusEnum.Withdrawn;
            await _uow.SaveChangeAsync();

            return Success(null, BusinessCode.UPDATE_SUCESSFULLY);
        }

        public async Task<ResponseDTO> ResubmitAsync(Guid listingId, Guid userId)
        {
            ResponseDTO dto = new ResponseDTO();

            try
            {
                var listing = await _listingRepo.AsQueryable()
                    .Include(l => l.Bikes)
                        .ThenInclude(b => b.Medias)
                    .FirstOrDefaultAsync(l => l.Id == listingId && l.UserId == userId);

                if (listing == null)
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.DATA_NOT_FOUND;
                    dto.Message = "Không tìm thấy listing.";
                    return dto;
                }

                if (listing.Status != ListingStatusEnum.Rejected)
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.INVALID_ACTION;
                    dto.Message = "Chỉ listing bị từ chối mới được resubmit.";
                    return dto;
                }

                // 🔥 BẮT BUỘC PHẢI SỬA TRƯỚC KHI RESUBMIT
                if (listing.UpdatedAt == null || listing.UpdatedAt == listing.CreatedAt)
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.INVALID_ACTION;
                    dto.Message = "Bạn phải chỉnh sửa listing trước khi resubmit.";
                    return dto;
                }

                var bike = listing.Bikes.FirstOrDefault();
                if (bike == null)
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.DATA_NOT_FOUND;
                    dto.Message = "Không tìm thấy bike.";
                    return dto;
                }

                // 🔥 VALIDATE MEDIA LẠI
                var medias = bike.Medias?
                    .Where(m => !m.IsDeleted)
                    .ToList();

                if (medias == null || !medias.Any())
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.INVALID_DATA;
                    dto.Message = "Phải có ít nhất 1 media.";
                    return dto;
                }

                if (!medias.Any(m => m.Image != null))
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.INVALID_DATA;
                    dto.Message = "Phải có ít nhất 1 ảnh.";
                    return dto;
                }

                // 🔥 RESET STATUS
                listing.Status = ListingStatusEnum.PendingInspection;
                listing.RejectReason = null;

                bike.Status = BikeStatusEnum.PendingInspection;

                await _uow.SaveChangeAsync();

                dto.IsSucess = true;
                dto.BusinessCode = BusinessCode.UPDATE_SUCESSFULLY;
                dto.Message = "Resubmit thành công.";
            }
            catch (Exception ex)
            {
                dto.IsSucess = false;
                dto.BusinessCode = BusinessCode.EXCEPTION;
                dto.Message = "Lỗi resubmit: " + ex.Message;
            }

            return dto;
        }
    }
}   