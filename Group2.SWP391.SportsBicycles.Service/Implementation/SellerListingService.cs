using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;

public class SellerListingService : ISellerListingService
{
    private readonly IGenericRepository<Listing> _listingRepo;
    private readonly IGenericRepository<Bike> _bikeRepo;
    private readonly IGenericRepository<Media> _mediaRepo;
    private readonly IGenericRepository<Order> _orderRepo;
    private readonly IUnitOfWork _uow;

    public SellerListingService(
        IGenericRepository<Listing> listingRepo,
        IGenericRepository<Bike> bikeRepo,
        IGenericRepository<Media> mediaRepo,
        IGenericRepository<Order> orderRepo,
        IUnitOfWork uow)
    {
        _listingRepo = listingRepo;
        _bikeRepo = bikeRepo;
        _mediaRepo = mediaRepo;
        _orderRepo = orderRepo;
        _uow = uow;
    }

    // ================= CREATE =================
    public async Task<ResponseDTO> CreateAsync(Guid sellerId, ListingCreateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SerialNumber))
            return Fail(BusinessCode.INVALID_DATA, "Thiếu serial");

        if (dto.Medias == null || !dto.Medias.Any())
            return Fail(BusinessCode.INVALID_DATA, "Thiếu media");

        var validCities = new[] { "Hà Nội", "TP.HCM", "Đà Nẵng" };
        if (!validCities.Contains(dto.City))
            return Fail(BusinessCode.INVALID_DATA, "City không hợp lệ");

        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            UserId = sellerId,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            SerialNumber = dto.SerialNumber,
            City = dto.City,
            Status = ListingStatusEnum.Draft,
            CreatedAt = DateTime.UtcNow
        };

        await _listingRepo.Insert(listing);

        var bike = new Bike
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            Brand = dto.Brand,
            Category = dto.Category,
            FrameSize = dto.FrameSize,
            Price = dto.Price,
            Status = BikeStatusEnum.PendingInspection
        };

        await _bikeRepo.Insert(bike);

        foreach (var m in dto.Medias)
        {
            await _mediaRepo.Insert(new Media
            {
                Id = Guid.NewGuid(),
                BikeId = bike.Id,
                Image = m.Image,
                VideoUrl = m.VideoUrl,
                Type = m.Type
            });
        }

        await _uow.SaveChangeAsync();
        return Success(listing.Id);
    }

    // ================= SUBMIT =================
    public async Task<ResponseDTO> SubmitForReviewAsync(Guid sellerId, Guid listingId)
    {
        var listing = await _listingRepo.AsQueryable()
            .Include(x => x.Bikes)
            .FirstOrDefaultAsync(x => x.Id == listingId && x.UserId == sellerId);

        if (listing == null)
            return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy");

        if (listing.Status != ListingStatusEnum.Draft)
            return Fail(BusinessCode.INVALID_ACTION, "Chỉ Draft mới submit");

        bool hasGroupset = await _mediaRepo.AsQueryable()
            .AnyAsync(m => m.Bike.ListingId == listingId && m.Type == MediaType.Groupset);

        if (!hasGroupset)
            return Fail(BusinessCode.INVALID_DATA, "Thiếu ảnh groupset");

        listing.Status = ListingStatusEnum.PendingReview;
        await _uow.SaveChangeAsync();

        return Success();
    }

    // ================= WITHDRAW =================
    public async Task<ResponseDTO> WithdrawAsync(Guid sellerId, Guid listingId)
    {
        var listing = await _listingRepo.GetByExpression(
            x => x.Id == listingId && x.UserId == sellerId);

        if (listing == null)
            return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy");

        // ✅ FIX: check active order đúng cách
        bool hasActiveOrder = await _orderRepo.AsQueryable()
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
                o.OrderItems.Any(oi => oi.Bike.ListingId == listingId)
            );

        if (hasActiveOrder)
            return Fail(BusinessCode.INVALID_ACTION, "Đang có đơn active");

        listing.Status = ListingStatusEnum.Withdrawn;
        await _uow.SaveChangeAsync();

        return Success();
    }

    // ================= UPDATE =================
    public async Task<ResponseDTO> UpdateAsync(Guid sellerId, Guid listingId, ListingUpsertDTO dto)
    {
        var listing = await _listingRepo.GetByExpression(
            x => x.Id == listingId && x.UserId == sellerId);

        if (listing == null)
            return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy");

        if (listing.Status == ListingStatusEnum.Published)
            return Fail(BusinessCode.INVALID_ACTION, "Không được sửa khi đã publish");

        listing.Title = dto.Title;
        listing.Description = dto.Description;

        await _uow.SaveChangeAsync();
        return Success();
    }

    // ================= DELETE =================
    public async Task<ResponseDTO> DeleteAsync(Guid sellerId, Guid listingId)
    {
        var listing = await _listingRepo.GetByExpression(
            x => x.Id == listingId && x.UserId == sellerId);

        if (listing == null)
            return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy");

        await _listingRepo.Delete(listing);
        await _uow.SaveChangeAsync();

        return Success();
    }

    // ================= GET BY ID =================
    public async Task<ResponseDTO> GetByIdAsync(Guid sellerId, Guid listingId)
    {
        var listing = await _listingRepo.GetByExpression(
            x => x.Id == listingId && x.UserId == sellerId);

        return Success(listing);
    }

    // ================= MY LIST =================
    public async Task<ResponseDTO> GetMyListingsAsync(Guid sellerId, int pageNumber, int pageSize)
    {
        var result = await _listingRepo.GetAllDataByExpression(
            x => x.UserId == sellerId,
            pageNumber,
            pageSize);

        return Success(result);
    }

    // ================= DETAILS =================
    public async Task<ResponseDTO> GetDetailsAsync(Guid sellerId, Guid listingId)
    {
        var listing = await _listingRepo.AsQueryable()
            .Include(x => x.Bikes)
            .FirstOrDefaultAsync(x => x.Id == listingId && x.UserId == sellerId);

        return Success(listing);
    }

    // ================= VALIDATE =================
    public async Task<ResponseDTO> ValidateListingAsync(Guid sellerId, Guid listingId)
    {
        var listing = await _listingRepo.GetByExpression(
            x => x.Id == listingId && x.UserId == sellerId);

        if (listing == null)
            return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy");

        if (string.IsNullOrEmpty(listing.SerialNumber))
            return Fail(BusinessCode.INVALID_DATA, "Thiếu serial");

        return Success();
    }

    // ================= COMMON =================
    private ResponseDTO Success(object? data = null)
        => new() { IsSucess = true, Data = data };

    private ResponseDTO Fail(BusinessCode code, string msg)
        => new() { IsSucess = false, BusinessCode = code, Message = msg };
}