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

    // ================= HELPER =================
    private Guid? GetUserId()
    {
        var id = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(id) ? null : Guid.Parse(id);
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

    private static bool IsOrderActiveForListing(OrderStatusEnum status)
    {
        return status == OrderStatusEnum.Pending
            || status == OrderStatusEnum.Paid
            || status == OrderStatusEnum.Confirmed
            || status == OrderStatusEnum.Shipping;
    }

    private static bool IsBuyerAllowedToViewSellerPII(OrderStatusEnum status)
    {
        // Theo SRS: chỉ lộ PII sau khi cọc/đơn đã được xác nhận hợp lệ.
        // Với enum hiện tại của bạn, dùng Paid trở lên là hợp lý nhất.
        return status == OrderStatusEnum.Paid
            || status == OrderStatusEnum.Confirmed
            || status == OrderStatusEnum.Shipping
            || status == OrderStatusEnum.Completed;
    }

    private async Task<List<Guid>> GetActiveListingIdsAsync()
    {
        return await _orderRepo.AsQueryable()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Bike)
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

        var items = bikes.Select(b => new BuyerBikeListingDTO
        {
            BikeId = b.Id,
            ListingId = b.ListingId,
            Title = b.Listing?.Title ?? string.Empty,
            Price = b.Price,
            Brand = b.Brand,
            Category = b.Category,
            Thumbnail = b.Medias?
                .OrderBy(m => m.Type)
                .Select(m => m.Image)
                .FirstOrDefault() ?? string.Empty,
            Overall = b.Overall,
            IsInspected = b.Inspection != null,
            IsWishlisted = currentUserId != null &&
                           b.Wishlists != null &&
                           b.Wishlists.Any(w => w.UserId == currentUserId.Value)
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

    // ================= DETAIL =================
    public async Task<ResponseDTO> GetDetail(Guid listingId)
    {
        var listing = await _listingRepo.AsQueryable()
            .Include(l => l.User)
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
        var activeListingIds = await GetActiveListingIdsAsync();

        // Nếu listing đang có giao dịch active thì vẫn có thể xem detail,
        // nhưng không nên cho đặt mua mới ở tầng khác.
        // Ở đây chỉ cần xử lý đúng phần hiển thị.
        bool hasQualifiedOrder = false;

        if (currentUserId != null)
        {
            hasQualifiedOrder = await _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                .AnyAsync(o =>
                    o.UserId == currentUserId.Value &&
                    IsBuyerAllowedToViewSellerPII(o.Status) &&
                    o.OrderItems.Any(oi => oi.Bike.ListingId == listingId));
        }

        var bikes = listing.Bikes?.Select(b => new BikeDetailDTO
        {
            BikeId = b.Id,
            Brand = b.Brand,
            Category = b.Category,
            Price = b.Price,
            FrameSize = b.FrameSize,
            Overall = b.Overall
        }).ToList() ?? new List<BikeDetailDTO>();

        return Success(new BuyerListingDetailDTO
        {
            ListingId = listing.Id,
            Title = listing.Title,
            Description = listing.Description,

            // Theo SRS: chỉ lộ thông tin seller sau khi buyer có order/cọc hợp lệ
            SellerName = hasQualifiedOrder ? listing.User?.FullName : null,

            Bikes = bikes
        });
    }

    // ================= SEARCH =================
    public async Task<ResponseDTO> SearchAsync(
        string? keyword,
        string? brand,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        string? frameSize,
        string? condition,
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
                b.Brand.Contains(keyword) ||
                b.Category.Contains(keyword));
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
            query = query.Where(b => b.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(b => b.Price <= maxPrice.Value);

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

        var totalItems = await query.CountAsync();

        var bikes = await query
            .OrderByDescending(b => b.Listing.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = bikes.Select(b => new BuyerBikeListingDTO
        {
            BikeId = b.Id,
            ListingId = b.ListingId,
            Title = b.Listing?.Title ?? string.Empty,
            Price = b.Price,
            Brand = b.Brand,
            Category = b.Category,
            Thumbnail = b.Medias?
                .OrderBy(m => m.Type)
                .Select(m => m.Image)
                .FirstOrDefault() ?? string.Empty,
            Overall = b.Overall,
            IsInspected = b.Inspection != null,
            IsWishlisted = currentUserId != null &&
                           b.Wishlists != null &&
                           b.Wishlists.Any(w => w.UserId == currentUserId.Value)
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

   


}