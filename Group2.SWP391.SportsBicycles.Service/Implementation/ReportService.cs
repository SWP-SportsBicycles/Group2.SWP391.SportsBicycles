using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class ReportService : IReportService
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Report> _reportRepo;
        private readonly IUnitOfWork _uow;

        public ReportService(
            IGenericRepository<Order> orderRepo,
            IGenericRepository<Report> reportRepo,
            IUnitOfWork uow)
        {
            _orderRepo = orderRepo;
            _reportRepo = reportRepo;
            _uow = uow;
        }

        private static ResponseDTO Success(
            object? data = null,
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

        public async Task<ResponseDTO> CreateReportAsync(Guid userId, Guid orderId, CreateReportDTO dto)
        {
            if (userId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            if (dto == null)
                return Fail(BusinessCode.INVALID_INPUT, "Dữ liệu không hợp lệ");

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return Fail(BusinessCode.INVALID_DATA, "Thiếu lý do report");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.Shipment)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            if (order.Status != OrderStatusEnum.Shipping &&
                order.Status != OrderStatusEnum.Delivered &&
                order.Status != OrderStatusEnum.Completed)
            {
                return Fail(BusinessCode.INVALID_ACTION, "Order chưa ở trạng thái có thể report");
            }

            var existingActiveReport = await _reportRepo.AsQueryable()
                .FirstOrDefaultAsync(r =>
                    r.OrderId == orderId &&
                    r.UserId == userId &&
                    (r.Status == ReportStatusEnum.Pending ||
                     r.Status == ReportStatusEnum.Reviewing));

            if (existingActiveReport != null)
                return Fail(BusinessCode.INVALID_ACTION, "Order này đã có report đang được xử lý");

            var report = new Report
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                UserId = userId,
                Type = dto.Type,
                Reason = dto.Reason.Trim(),
                Status = ReportStatusEnum.Pending
            };

            await _reportRepo.Insert(report);

            order.Status = OrderStatusEnum.Disputed;

            await _uow.SaveChangeAsync();

            return Success(new
            {
                ReportId = report.Id,
                OrderId = report.OrderId,
                UserId = report.UserId,
                Type = report.Type.ToString(),
                Reason = report.Reason,
                ReportStatus = report.Status.ToString(),
                OrderStatus = order.Status.ToString()
            }, BusinessCode.CREATED_SUCCESSFULLY);
        }

        public async Task<ResponseDTO> GetMyReportsAsync(Guid userId, int pageNumber, int pageSize)
        {
            if (userId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (pageNumber <= 0 || pageSize <= 0)
                return Fail(BusinessCode.INVALID_INPUT, "Thông tin phân trang không hợp lệ");

            var query = _reportRepo.AsQueryable()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt);

            var totalItems = await query.CountAsync();

            var reports = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = reports.Select(r => new ReportListItemDTO
            {
                ReportId = r.Id,
                OrderId = r.OrderId,
                Type = r.Type.ToString(),
                Reason = r.Reason,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
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

        public async Task<ResponseDTO> GetReportDetailAsync(Guid userId, Guid reportId)
        {
            if (userId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (reportId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "ReportId không hợp lệ");

            var report = await _reportRepo.AsQueryable()
                .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == userId);

            if (report == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy report");

            var dto = new ReportDetailDTO
            {
                ReportId = report.Id,
                OrderId = report.OrderId,
                UserId = report.UserId,
                Type = report.Type.ToString(),
                Reason = report.Reason,
                Status = report.Status.ToString(),
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };

            return Success(dto);
        }
    }
}
