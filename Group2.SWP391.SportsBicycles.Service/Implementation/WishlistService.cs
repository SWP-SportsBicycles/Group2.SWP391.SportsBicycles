using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class WishlistService : IWishlistService
    {
        private readonly IGenericRepository<Wishlist> _wishlistRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IHttpContextAccessor _http;

        public WishlistService(
            IGenericRepository<Wishlist> wishlistRepo,
            IGenericRepository<Bike> bikeRepo,
            IHttpContextAccessor http)
        {
            _wishlistRepo = wishlistRepo;
            _bikeRepo = bikeRepo;
            _http = http;
        }

        // ================= HELPER =================
        private Guid? GetUserId()
        {
            var id = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(id) ? null : Guid.Parse(id);
        }

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

        // ================= ADD =================
        public async Task<ResponseDTO> AddToWishlistAsync(Guid bikeId)
        {
            var userId = GetUserId();
            if (userId == null)
                return Fail(BusinessCode.ACCESS_DENIED, "Chưa đăng nhập");

            var bike = await _bikeRepo.AsQueryable()
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == bikeId);

            if (bike == null || bike.Listing == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Bike không tồn tại");

            if (bike.Listing.Status != ListingStatusEnum.Published)
                return Fail(BusinessCode.INVALID_ACTION, "Bike chưa được publish");

            var exists = await _wishlistRepo.AsQueryable()
                .AnyAsync(w => w.UserId == userId.Value && w.BikeId == bikeId);

            if (exists)
                return Fail(BusinessCode.DUPLICATE_DATA, "Bike đã có trong wishlist");

            await _wishlistRepo.Insert(new Wishlist
            {
                UserId = userId.Value,
                BikeId = bikeId
            });

            await _wishlistRepo.GetDbContext().SaveChangesAsync();

            return new ResponseDTO
            {
                IsSucess = true,
                BusinessCode = BusinessCode.CREATED_SUCCESSFULLY,
                Message = "Đã thêm vào danh sách yêu thích"
            };
        }

        // ================= REMOVE =================
        public async Task<ResponseDTO> RemoveFromWishlistAsync(Guid bikeId)
        {
            var userId = GetUserId();
            if (userId == null)
                return Fail(BusinessCode.ACCESS_DENIED, "Chưa đăng nhập");

            var item = await _wishlistRepo.GetFirstByExpression(
                w => w.UserId == userId.Value && w.BikeId == bikeId);

            if (item == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Bike không có trong wishlist");

            await _wishlistRepo.Delete(item);
            await _wishlistRepo.GetDbContext().SaveChangesAsync();

            return new ResponseDTO
            {
                IsSucess = true,
                BusinessCode = BusinessCode.DELETE_SUCESSFULLY,
                Message = "Đã xoá khỏi danh sách yêu thích"
            };
        }

        // ================= GET MY WISHLIST =================
        public async Task<ResponseDTO> GetMyWishlistAsync(int pageNumber, int pageSize)
        {
            var userId = GetUserId();
            if (userId == null)
                return Fail(BusinessCode.ACCESS_DENIED, "Chưa đăng nhập");

            if (pageNumber <= 0 || pageSize <= 0)
                return Fail(BusinessCode.INVALID_INPUT, "Invalid pagination");

            var query = _wishlistRepo.AsQueryable()
                .Include(w => w.Bike)
                    .ThenInclude(b => b.Listing)
                .Include(w => w.Bike)
                    .ThenInclude(b => b.Medias)
                .Include(w => w.Bike)
                    .ThenInclude(b => b.Inspection)
                .Where(w =>
                    w.UserId == userId.Value &&
                    w.Bike != null &&
                    w.Bike.Listing != null &&
                    w.Bike.Listing.Status == ListingStatusEnum.Published);

            var totalItems = await query.CountAsync();

            var wishlistItems = await query
                .OrderByDescending(w => w.Bike.Listing.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = wishlistItems.Select(w => new BuyerBikeListingDTO
            {
                BikeId = w.BikeId,
                ListingId = w.Bike.ListingId,
                Title = w.Bike.Listing.Title,
                Price = w.Bike.Price,
                Brand = w.Bike.Brand,
                Category = w.Bike.Category,
                Thumbnail = w.Bike.Medias?
                    .OrderBy(m => m.Type)
                    .Select(m => m.Image)
                    .FirstOrDefault() ?? string.Empty,
                Overall = w.Bike.Overall,
                IsInspected = w.Bike.Inspection != null,
                IsWishlisted = true
            }).ToList();

            return new ResponseDTO
            {
                IsSucess = true,
                BusinessCode = BusinessCode.GET_DATA_SUCCESSFULLY,
                Message = totalItems == 0
          ? "Danh sách yêu thích trống"
          : "Lấy danh sách yêu thích thành công",
                Data = new
                {
                    Items = items,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                    PageNumber = pageNumber,
                    PageSize = pageSize
                }
            };
        }
    }
}