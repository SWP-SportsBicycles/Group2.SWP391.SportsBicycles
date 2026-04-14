using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Group2.SWP391.SportsBicycles.Services.Contract;

public class BuyerListingService : IBuyerListingService
{
    private readonly IGenericRepository<Bike> _bikeRepo;
    private readonly IGenericRepository<Listing> _listingRepo;
    private readonly IGenericRepository<Wishlist> _wishlistRepo;
    private readonly IGenericRepository<Order> _orderRepo;
    private readonly IHttpContextAccessor _http;

    public BuyerListingService(
        IGenericRepository<Bike> bikeRepo,
        IGenericRepository<Listing> listingRepo,
        IGenericRepository<Wishlist> wishlistRepo,
        IGenericRepository<Order> orderRepo,
        IHttpContextAccessor http)
    {
        _bikeRepo = bikeRepo;
        _listingRepo = listingRepo;
        _wishlistRepo = wishlistRepo;
        _orderRepo = orderRepo;
        _http = http;
    }

    // ================= HELPER =================
    private Guid? GetUserId()
    {
        var id = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(id) ? null : Guid.Parse(id);
    }

    private static ResponseDTO Fail(BusinessCode code, string msg)
        => new() { IsSucess = false, BusinessCode = code, Message = msg };

    // ================= ACTIVE LISTING =================
    private async Task<List<Guid>> GetActiveListingIds()
    {
        return await _orderRepo.AsQueryable()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Bike)
            .Where(o =>
                o.Status == OrderStatusEnum.Pending ||
                o.Status == OrderStatusEnum.Paid ||
                o.Status == OrderStatusEnum.Confirmed ||
                o.Status == OrderStatusEnum.Shipping
            )
            .SelectMany(o => o.OrderItems)
            .Select(oi => oi.Bike.ListingId)
            .Distinct()
            .ToListAsync();
    }

    // ================= GET ALL =================
    public async Task<ResponseDTO> GetAllAsync(int pageNumber, int pageSize)
    {
        if (pageNumber <= 0 || pageSize <= 0)
            return Fail(BusinessCode.INVALID_INPUT, "Invalid pagination");

        var currentUserId = GetUserId();

        var activeListingIds = await GetActiveListingIds();

        var query = _bikeRepo.AsQueryable()
            .Include(b => b.Listing)
            .Include(b => b.Medias)
            .Include(b => b.Wishlists)
            .Include(b => b.Inspection)
            .Where(b =>
                b.Status == BikeStatusEnum.Available &&
                b.Listing.Status == ListingStatusEnum.Published &&
                !activeListingIds.Contains(b.ListingId)
            );

        var total = await query.CountAsync();

        var bikes = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var data = bikes.Select(b => new BuyerBikeListingDTO
        {
            BikeId = b.Id,
            ListingId = b.ListingId,
            Title = b.Listing.Title,
            Price = b.Price,
            Brand = b.Brand,
            Category = b.Category,
            Thumbnail = b.Medias.FirstOrDefault()?.Image ?? "",
            Overall = b.Overall,
            IsInspected = b.Inspection != null && b.Inspection.Score >= 3,
            IsWishlisted = currentUserId != null &&
                           b.Wishlists.Any(w => w.UserId == currentUserId)
        });

        return new ResponseDTO
        {
            IsSucess = true,
            BusinessCode = BusinessCode.GET_DATA_SUCCESSFULLY,
            Data = new
            {
                Items = data,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        };
    }

    // ================= DETAIL =================
    public async Task<ResponseDTO> GetDetail(Guid listingId)
    {
        var listing = await _listingRepo.GetByExpression(
            l => l.Id == listingId,
            l => l.User,
            l => l.Bikes
        );

        if (listing == null)
            return Fail(BusinessCode.DATA_NOT_FOUND, "Listing không tồn tại");

        var currentUserId = GetUserId();

        bool hasDeposited = false;

        if (currentUserId != null)
        {
            hasDeposited = await _orderRepo.AsQueryable()
     .Include(o => o.OrderItems)
         .ThenInclude(oi => oi.Bike)
     .AnyAsync(o =>
         o.UserId == currentUserId &&
         (
             o.Status == OrderStatusEnum.Paid ||
             o.Status == OrderStatusEnum.Confirmed ||
             o.Status == OrderStatusEnum.Shipping ||
             o.Status == OrderStatusEnum.Completed
         ) &&
         o.OrderItems.Any(oi => oi.Bike.ListingId == listingId)
     );
        }

        var bikes = await _bikeRepo.GetListByExpression(b => b.ListingId == listingId);

        return new ResponseDTO
        {
            IsSucess = true,
            BusinessCode = BusinessCode.GET_DATA_SUCCESSFULLY,
            Data = new BuyerListingDetailDTO
            {
                ListingId = listing.Id,
                Title = listing.Title,
                Description = listing.Description,

                // 🔐 PII
                SellerName = hasDeposited ? listing.User.FullName : null,

                Bikes = bikes.Select(b => new BikeDetailDTO
                {
                    BikeId = b.Id,
                    Brand = b.Brand,
                    Category = b.Category,
                    Price = b.Price,
                    FrameSize = b.FrameSize,
                    Overall = b.Overall
                }).ToList()
            }
        };
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
        var activeListingIds = await GetActiveListingIds();

        var query = _bikeRepo.AsQueryable()
            .Include(b => b.Listing)
            .Where(b =>
                b.Status == BikeStatusEnum.Available &&
                b.Listing.Status == ListingStatusEnum.Published &&
                !activeListingIds.Contains(b.ListingId)
            );

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(b => b.Listing.Title.Contains(keyword));

        if (!string.IsNullOrWhiteSpace(brand))
            query = query.Where(b => b.Brand == brand);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(b => b.Category == category);

        if (minPrice.HasValue)
            query = query.Where(b => b.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(b => b.Price <= maxPrice.Value);

        if (!string.IsNullOrWhiteSpace(frameSize))
            query = query.Where(b => b.FrameSize == frameSize);

        if (!string.IsNullOrWhiteSpace(condition))
            query = query.Where(b => b.Condition == condition);

        var total = await query.CountAsync();

        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BuyerBikeListingDTO
            {
                BikeId = b.Id,
                ListingId = b.ListingId,
                Title = b.Listing.Title,
                Price = b.Price
            })
            .ToListAsync();

        return new ResponseDTO
        {
            IsSucess = true,
            BusinessCode = BusinessCode.GET_DATA_SUCCESSFULLY,
            Data = new
            {
                Items = data,
                Total = total
            }
        };
    }

    // ================= WISHLIST =================
    public async Task<ResponseDTO> AddToWishlistAsync(Guid bikeId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Fail(BusinessCode.ACCESS_DENIED, "Chưa đăng nhập");

        var exists = await _wishlistRepo.AsQueryable()
            .AnyAsync(x => x.UserId == userId && x.BikeId == bikeId);

        if (exists)
            return Fail(BusinessCode.DUPLICATE_DATA, "Đã tồn tại");

        await _wishlistRepo.Insert(new Wishlist
        {
            UserId = userId.Value,
            BikeId = bikeId
        });

        return new ResponseDTO
        {
            IsSucess = true,
            BusinessCode = BusinessCode.CREATED_SUCCESSFULLY
        };
    }

    public async Task<ResponseDTO> RemoveFromWishlistAsync(Guid bikeId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Fail(BusinessCode.ACCESS_DENIED, "Chưa đăng nhập");

        var item = await _wishlistRepo.GetFirstByExpression(
            x => x.UserId == userId && x.BikeId == bikeId);

        if (item == null)
            return Fail(BusinessCode.DATA_NOT_FOUND, "Không tồn tại");

        await _wishlistRepo.Delete(item);

        return new ResponseDTO
        {
            IsSucess = true,
            BusinessCode = BusinessCode.DELETE_SUCESSFULLY
        };
    }

    public async Task<ResponseDTO> GetMyWishlistAsync(int pageNumber, int pageSize)
    {
        var userId = GetUserId();
        if (userId == null)
            return Fail(BusinessCode.ACCESS_DENIED, "Chưa đăng nhập");

        var result = await _wishlistRepo.GetAllDataByExpression(
            filter: w => w.UserId == userId,
            pageNumber: pageNumber,
            pageSize: pageSize,
            includes: w => w.Bike
        );

        var data = (result.Items ?? new List<Wishlist>())
            .Select(w => new BuyerBikeListingDTO
            {
                BikeId = w.BikeId,
                ListingId = w.Bike.ListingId,
                Title = w.Bike.Listing.Title,
                Price = w.Bike.Price
            });

        return new ResponseDTO
        {
            IsSucess = true,
            BusinessCode = BusinessCode.GET_DATA_SUCCESSFULLY,
            Data = new
            {
                Items = data,
                TotalPages = result.TotalPages
            }
        };
    }
}