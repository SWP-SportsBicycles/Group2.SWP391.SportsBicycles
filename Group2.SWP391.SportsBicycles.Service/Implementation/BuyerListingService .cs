using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class BuyerListingService : IBuyerListingService
{
    private readonly IGenericRepository<Bike> _bikeRepo;
    private readonly IGenericRepository<Listing> _listingRepo;
    private readonly IGenericRepository<Order> _orderRepo;
    private readonly IHttpContextAccessor _http;

    public BuyerListingService(
        IGenericRepository<Bike> bikeRepo,
        IGenericRepository<Listing> listingRepo,
        IGenericRepository<Order> orderRepo,
        IHttpContextAccessor http)
    {
        _bikeRepo = bikeRepo;
        _listingRepo = listingRepo;
        _orderRepo = orderRepo;
        _http = http;
    }

    private Guid? GetUserId()
    {
        var id = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var userId) ? userId : null;
    }

    private static ResponseDTO Success(object? data = null)
        => new()
        {
            IsSucess = true,
            BusinessCode = BusinessCode.GET_DATA_SUCCESSFULLY,
            Data = data
        };

    private static ResponseDTO Fail(BusinessCode code, string msg)
        => new()
        {
            IsSucess = false,
            BusinessCode = code,
            Message = msg
        };

    private async Task<List<Guid>> GetActiveListingIdsAsync()
    {
        return await _orderRepo.AsQueryable()
            .Where(o =>
                o.Status == OrderStatusEnum.Pending ||
                o.Status == OrderStatusEnum.Paid ||
                o.Status == OrderStatusEnum.Confirmed ||
                o.Status == OrderStatusEnum.Shipping)
            .SelectMany(o => o.OrderItems)
            .Where(oi => oi.Bike != null)
            .Select(oi => oi.Bike.ListingId)
            .Distinct()
            .ToListAsync();
    }

    private IQueryable<Bike> BuildPublicBuyerQuery(List<Guid> activeListingIds)
    {
        return _bikeRepo.AsQueryable()
            .Include(b => b.Listing)
                .ThenInclude(l => l.User)
            .Include(b => b.Medias)
            .Include(b => b.Wishlists)
            .Include(b => b.Inspection)
            .Where(b =>
                b.Listing != null &&
                b.Status == BikeStatusEnum.Available &&
                b.Listing.Status == ListingStatusEnum.Published &&
                !activeListingIds.Contains(b.ListingId));
    }

    // ===== MEDIA HELPER =====
    private static string GetThumbnail(Bike bike)
    {
        return bike.Medias?
            .Where(m => !string.IsNullOrWhiteSpace(m.Image))
            .OrderBy(m => m.Type)
            .Select(m => m.Image!)
            .FirstOrDefault()
            ?? string.Empty;
    }

    private static List<string> GetImages(Bike bike)
    {
        return bike.Medias?
            .Where(m => !string.IsNullOrWhiteSpace(m.Image))
            .OrderBy(m => m.Type)
            .Select(m => m.Image!)
            .Distinct()
            .ToList()
            ?? new List<string>();
    }

    private static List<string> GetVideoUrls(Bike bike)
    {
        return bike.Medias?
            .Where(m => !string.IsNullOrWhiteSpace(m.VideoUrl))
            .OrderBy(m => m.Type)
            .Select(m => m.VideoUrl!)
            .Distinct()
            .ToList()
            ?? new List<string>();
    }

    private static BuyerBikeListingDTO MapToBuyerBikeListingDTO(Bike b, Guid? currentUserId)
    {
        return new BuyerBikeListingDTO
        {
            BikeId = b.Id,
            ListingId = b.ListingId,
            Title = b.Listing?.Title ?? string.Empty,
            Price = b.SalePrice,
            Brand = b.Brand,
            Category = b.Category,
            City = b.City, // 👈 ADD

            Thumbnail = GetThumbnail(b),
            Overall = b.Overall,
            IsInspected = b.Inspection != null,
            IsWishlisted = currentUserId != null &&
                           b.Wishlists != null &&
                           b.Wishlists.Any(w => w.UserId == currentUserId.Value)
        };
    }

    private static BikeDetailDTO MapToBikeDetailDTO(Bike b)
    {
        return new BikeDetailDTO
        {
            BikeId = b.Id,
            SerialNumber = b.SerialNumber,
            Brand = b.Brand,
            Category = b.Category,
            Price = b.SalePrice,
            FrameSize = b.FrameSize,
            FrameMaterial = b.FrameMaterial,
            Paint = b.Paint,
            Groupset = b.Groupset,
            Operating = b.Operating,
            TireRim = b.TireRim,
            BrakeType = b.BrakeType,
            Overall = b.Overall,
            Condition = b.Condition,
            City = b.City,
            Status = b.Status.ToString(),

            Thumbnail = GetThumbnail(b),
            Images = GetImages(b),
            VideoUrls = GetVideoUrls(b)
        };
    }

    // ================= LIST =================
    public async Task<ResponseDTO> GetAllAsync(int pageNumber, int pageSize)
    {
        if (pageNumber <= 0 || pageSize <= 0)
            return Fail(BusinessCode.INVALID_INPUT, "Invalid pagination");

        var currentUserId = GetUserId();
        var activeListingIds = await GetActiveListingIdsAsync();

        var query = BuildPublicBuyerQuery(activeListingIds);

        var totalItems = await query.CountAsync();

        var bikes = await query
            .OrderByDescending(b => b.Listing.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = bikes
            .Select(b => MapToBuyerBikeListingDTO(b, currentUserId))
            .ToList();

        return Success(new
        {
            Items = items,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    // ================= DETAIL =================
    public async Task<ResponseDTO> GetDetail(Guid listingId)
    {
        var listing = await _listingRepo.AsQueryable()
            .Include(l => l.User)
                .ThenInclude(u => u.SellerShippingProfiles)
            .Include(l => l.Bikes)
                .ThenInclude(b => b.Medias)
            .Include(l => l.Bikes)
                .ThenInclude(b => b.Inspection)
            .FirstOrDefaultAsync(l =>
                l.Id == listingId &&
                l.Status == ListingStatusEnum.Published);

        if (listing == null)
            return Fail(BusinessCode.DATA_NOT_FOUND, "Listing không tồn tại");

        var currentUserId = GetUserId();

        var bikes = listing.Bikes?
            .Where(b => b.Status == BikeStatusEnum.Available)
            .Select(MapToBikeDetailDTO)
            .ToList() ?? new List<BikeDetailDTO>();

        var isWishlisted = false;
        if (currentUserId != null)
        {
            isWishlisted = await _bikeRepo.AsQueryable()
                .AnyAsync(b => b.ListingId == listingId &&
                               b.Wishlists.Any(w => w.UserId == currentUserId.Value));
        }

        var sellerProfile = listing.User?.SellerShippingProfiles?
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        var sellerName = !string.IsNullOrWhiteSpace(sellerProfile?.SenderName)
            ? sellerProfile!.SenderName
            : listing.User?.FullName ?? string.Empty;

        return Success(new BuyerListingDetailDTO
        {
            ListingId = listing.Id,
            Title = listing.Title,
            Description = listing.Description,
            SellerName = sellerName,
            IsWishlisted = isWishlisted,
            Bikes = bikes
        });
    }

    public async Task<ResponseDTO> SearchAsync(
        string? keyword,
        string? brand,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        string? frameSize,
        string? condition,
        string? city,
        int pageNumber,
        int pageSize)
    {
        if (pageNumber <= 0 || pageSize <= 0)
            return Fail(BusinessCode.INVALID_INPUT, "Invalid pagination");

        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            return Fail(BusinessCode.INVALID_INPUT, "Khoảng giá không hợp lệ");

        var currentUserId = GetUserId();
        var activeListingIds = await GetActiveListingIdsAsync();

        var query = BuildPublicBuyerQuery(activeListingIds);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            query = query.Where(b =>
                b.Listing.Title.Contains(keyword) ||
                b.Listing.Description.Contains(keyword) ||
                b.Brand.Contains(keyword) ||
                b.Category.Contains(keyword) ||
                b.FrameSize.Contains(keyword) ||
                b.FrameMaterial.Contains(keyword) ||
                b.City.Contains(keyword) ||
                b.Condition.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            brand = brand.Trim();
            query = query.Where(b => b.Brand == brand);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            category = category.Trim();
            query = query.Where(b => b.Category == category);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(b => b.SalePrice >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(b => b.SalePrice <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(frameSize))
        {
            frameSize = frameSize.Trim();
            query = query.Where(b => b.FrameSize == frameSize);
        }

        if (!string.IsNullOrWhiteSpace(condition))
        {
            condition = condition.Trim();
            query = query.Where(b => b.Condition == condition);
        }

        // FIX: city filter đang thiếu
        if (!string.IsNullOrWhiteSpace(city))
        {
            city = city.Trim();
            query = query.Where(b => b.City == city);
        }

        var totalItems = await query.CountAsync();

        var bikes = await query
            .OrderByDescending(b => b.Listing.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = bikes
            .Select(b => MapToBuyerBikeListingDTO(b, currentUserId))
            .ToList();

        return Success(new
        {
            Items = items,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }
}