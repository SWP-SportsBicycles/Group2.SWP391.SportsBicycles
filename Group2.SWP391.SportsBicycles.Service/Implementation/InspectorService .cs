using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;

public class InspectorService : IInspectorService
{
    private readonly IGenericRepository<Inspection> _inspectionRepo;
    private readonly IGenericRepository<Order> _orderRepo;
    private readonly IUnitOfWork _uow;

    public InspectorService(
        IGenericRepository<Inspection> inspectionRepo,
        IGenericRepository<Order> orderRepo,
        IUnitOfWork uow)
    {
        _inspectionRepo = inspectionRepo;
        _orderRepo = orderRepo;
        _uow = uow;
    }

    // ================= SUBMIT =================
    public async Task<ResponseDTO> SubmitInspectionAsync(Guid inspectorId, Guid orderId, InspectionDTO dto)
    {
        var order = await _orderRepo.AsQueryable()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Bike)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return Fail("Order không tồn tại");

        var bike = order.OrderItems.FirstOrDefault()?.Bike;

        if (bike == null)
            return Fail("Không tìm thấy bike");

        var inspection = new Inspection
        {
            Id = Guid.NewGuid(),
            UserId = inspectorId,
            Bike = bike,
            Frame = dto.Frame,
            PaintCondition = dto.PaintCondition,
            Drivetrain = dto.Drivetrain,
            Brakes = dto.Brakes,
            Score = dto.Score,
            Comment = dto.Comment,
            InspectionDate = DateTime.UtcNow
        };

        await _inspectionRepo.Insert(inspection);

        // ✅ Update Bike theo SRS
        bike.Status = dto.Score >= 3
            ? BikeStatusEnum.Available
            : BikeStatusEnum.Disabled;

        await _uow.SaveChangeAsync();

        return Success();
    }

    // ================= PENDING =================
    public async Task<ResponseDTO> GetPendingInspectionsAsync(int pageNumber, int pageSize)
    {
        var result = await _orderRepo.GetAllDataByExpression(
            o => o.Status == OrderStatusEnum.Paid,
            pageNumber,
            pageSize,
            includes: o => o.OrderItems
        );

        return new ResponseDTO
        {
            IsSucess = true,
            Data = result
        };
    }

    public async Task<ResponseDTO> GetInspectionDetailAsync(Guid orderId)
    {
        var order = await _orderRepo.AsQueryable()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Bike)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        return Success(order);
    }

    // ================= HISTORY =================
    public async Task<ResponseDTO> GetInspectionHistoryAsync(int pageNumber, int pageSize)
    {
        var result = await _inspectionRepo.GetAllDataByExpression(
            null,
            pageNumber,
            pageSize);

        return Success(result);
    }

    public async Task<ResponseDTO> GetInspectionHistoryDetailAsync(Guid inspectionId)
    {
        var inspection = await _inspectionRepo.GetById(inspectionId);
        return Success(inspection);
    }

    // ================= COMMON =================
    private ResponseDTO Success(object? data = null)
        => new() { IsSucess = true, Data = data };

    private ResponseDTO Fail(string msg)
        => new() { IsSucess = false, Message = msg };
}