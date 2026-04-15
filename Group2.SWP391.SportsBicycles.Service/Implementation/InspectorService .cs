using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class InspectorService : IInspectorService
    {
        private readonly IGenericRepository<Inspection> _inspectionRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IUnitOfWork _uow;

        public InspectorService(
            IGenericRepository<Inspection> inspectionRepo,
            IGenericRepository<Order> orderRepo,
            IGenericRepository<Bike> bikeRepo,
            IUnitOfWork uow)
        {
            _inspectionRepo = inspectionRepo;
            _orderRepo = orderRepo;
            _bikeRepo = bikeRepo;
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

        // ================= SUBMIT =================
        public async Task<ResponseDTO> SubmitInspectionAsync(Guid inspectorId, Guid orderId, InspectionDTO dto)
        {
            if (dto == null)
                return Fail(BusinessCode.INVALID_DATA, "Dữ liệu không hợp lệ");

            if (dto.Score < 0 || dto.Score > 10)
                return Fail(BusinessCode.INVALID_DATA, "Score không hợp lệ");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                .ThenInclude(b => b.Inspection)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Order không tồn tại");

            if (order.Status != OrderStatusEnum.Paid)
                return Fail(BusinessCode.INVALID_ACTION, "Order không ở trạng thái chờ inspection");

            var bike = order.OrderItems.FirstOrDefault()?.Bike;
            if (bike == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy bike");

            if (bike.InspectionId != null || bike.Inspection != null)
                return Fail(BusinessCode.INVALID_ACTION, "Bike đã được inspection");

            var inspection = new Inspection
            {
                Id = Guid.NewGuid(),
                UserId = inspectorId,
                Frame = dto.Frame,
                PaintCondition = dto.PaintCondition,
                Drivetrain = dto.Drivetrain,
                Brakes = dto.Brakes,
                Score = dto.Score,
                Comment = dto.Comment,
                InspectionDate = DateTime.UtcNow
            };

            await _inspectionRepo.Insert(inspection);

            bike.InspectionId = inspection.Id;
            bike.Overall = dto.Score.ToString();

            await _uow.SaveChangeAsync();

            return Success(null, BusinessCode.CREATED_SUCCESSFULLY);
        }

        // ================= PENDING =================
        public async Task<ResponseDTO> GetPendingInspectionsAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0 || pageSize <= 0)
                return Fail(BusinessCode.INVALID_INPUT, "Invalid pagination");

            var query = _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                .Where(o =>
                    o.Status == OrderStatusEnum.Paid &&
                    o.OrderItems.Any(oi => oi.Bike != null && oi.Bike.InspectionId == null))
                .OrderByDescending(o => o.CreatedAt);

            var totalItems = await query.CountAsync();

            var orders = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Success(new
            {
                Items = orders,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        public async Task<ResponseDTO> GetInspectionDetailAsync(Guid orderId)
        {
            var order = await _orderRepo.AsQueryable()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Bike)
                .ThenInclude(b => b.Medias)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Order không tồn tại");

            return Success(order);
        }

        // ================= HISTORY =================
        public async Task<ResponseDTO> GetInspectionHistoryAsync(Guid inspectorId, int pageNumber, int pageSize)
        {
            if (pageNumber <= 0 || pageSize <= 0)
                return Fail(BusinessCode.INVALID_INPUT, "Invalid pagination");

            var query = _inspectionRepo.AsQueryable()
                .Where(i => i.UserId == inspectorId)
                .OrderByDescending(i => i.InspectionDate);

            var totalItems = await query.CountAsync();

            var inspections = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Success(new
            {
                Items = inspections,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        public async Task<ResponseDTO> GetInspectionHistoryDetailAsync(Guid inspectionId)
        {
            var inspection = await _inspectionRepo.GetById(inspectionId);

            if (inspection == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Inspection không tồn tại");

            return Success(inspection);
        }
    }
}