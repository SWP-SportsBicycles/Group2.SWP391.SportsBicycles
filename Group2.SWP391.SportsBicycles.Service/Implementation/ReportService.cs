using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class ReportService : IReportService
    {
        private readonly IGenericRepository<Report> _reportRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IUnitOfWork _uow;

        public ReportService(
            IGenericRepository<Report> reportRepo,
            IGenericRepository<Order> orderRepo,
            IUnitOfWork uow)
        {
            _reportRepo = reportRepo;
            _orderRepo = orderRepo;
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

        private static string MapReportType(ReportTypeEnum type)
        {
            return type switch
            {
                ReportTypeEnum.WrongDescription => "Không đúng mô tả",
                ReportTypeEnum.ProductDefect => "Hư hỏng / Lỗi sản phẩm",
                ReportTypeEnum.MissingOrWrongItem => "Thiếu phụ kiện / Sai hàng",
                ReportTypeEnum.ShippingIssue => "Vấn đề vận chuyển",
                ReportTypeEnum.Other => "Khác",
                _ => "Khác"
            };
        }

        public async Task<ResponseDTO> CreateReportAsync(Guid buyerId, Guid orderId, CreateReportDTO dto)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (orderId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "OrderId không hợp lệ");

            if (dto == null)
                return Fail(BusinessCode.INVALID_INPUT, "Dữ liệu không hợp lệ");

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return Fail(BusinessCode.INVALID_DATA, "Lý do report không được để trống");

            var order = await _orderRepo.AsQueryable()
                .Include(o => o.Reports)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == buyerId);

            if (order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order");

            if (order.Status != OrderStatusEnum.Completed)
                return Fail(BusinessCode.INVALID_ACTION, "Chỉ được report sau khi đã xác nhận nhận hàng");

            var hasOpenReport = order.Reports.Any(r =>
                r.Status == ReportStatusEnum.Pending ||
                r.Status == ReportStatusEnum.Reviewing);

            if (hasOpenReport)
                return Fail(BusinessCode.INVALID_ACTION, "Order đã có report đang được xử lý");

            var report = new Report
            {
                Id = Guid.NewGuid(),
                Type = dto.Type,
                Reason = dto.Reason.Trim(),
                Description = dto.Description?.Trim(),
                VideoUrl = dto.VideoUrl?.Trim(),
                Status = ReportStatusEnum.Pending,
                OrderId = order.Id,
                UserId = buyerId
            };

            await _reportRepo.Insert(report);
            await _uow.SaveChangeAsync();

            return Success(new
            {
                ReportId = report.Id,
                report.OrderId,
                report.UserId,
                Type = MapReportType(report.Type),
                TypeCode = (int)report.Type,
                TypeKey = report.Type.ToString(),
                Status = report.Status.ToString(),
                report.Reason,
                report.Description,
                report.VideoUrl,
                report.CreatedAt
            }, BusinessCode.CREATED_SUCCESSFULLY);
        }

        public async Task<ResponseDTO> GetMyReportsAsync(Guid buyerId)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            var reports = await _reportRepo.AsQueryable()
                .Include(r => r.Order)
                .Where(r => r.UserId == buyerId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    ReportId = r.Id,
                    r.OrderId,
                    OrderStatus = r.Order.Status.ToString(),
                    Type = MapReportType(r.Type),
                    TypeCode = (int)r.Type,
                    TypeKey = r.Type.ToString(),
                    Status = r.Status.ToString(),
                    r.Reason,
                    r.Description,
                    r.VideoUrl,
                    r.CreatedAt
                })
                .ToListAsync();

            return Success(reports);
        }

        public async Task<ResponseDTO> GetReportDetailAsync(Guid buyerId, Guid reportId)
        {
            if (buyerId == Guid.Empty)
                return Fail(BusinessCode.AUTH_NOT_FOUND, "Không xác định được người dùng");

            if (reportId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "ReportId không hợp lệ");

            var report = await _reportRepo.AsQueryable()
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == buyerId);

            if (report == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy report");

            return Success(new
            {
                ReportId = report.Id,
                report.OrderId,
                report.UserId,
                OrderStatus = report.Order.Status.ToString(),
                Type = MapReportType(report.Type),
                TypeCode = (int)report.Type,
                TypeKey = report.Type.ToString(),
                Status = report.Status.ToString(),
                report.Reason,
                report.Description,
                report.VideoUrl,
                report.CreatedAt,
                report.UpdatedAt
            });
        }

        public async Task<ResponseDTO> GetReportsForInspectorAsync(int page, int size, string? status, string? type)
        {
            if (page <= 0 || size <= 0)
                return Fail(BusinessCode.INVALID_INPUT, "Thông tin phân trang không hợp lệ");

            var query = _reportRepo.AsQueryable()
                .Include(r => r.Order)
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ReportStatusEnum>(status, true, out var reportStatus))
            {
                query = query.Where(r => r.Status == reportStatus);
            }

            if (!string.IsNullOrWhiteSpace(type) &&
                Enum.TryParse<ReportTypeEnum>(type, true, out var reportType))
            {
                query = query.Where(r => r.Type == reportType);
            }

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(r => new
                {
                    ReportId = r.Id,
                    r.OrderId,
                    BuyerId = r.UserId,
                    BuyerName = r.User.FullName,
                    Type = MapReportType(r.Type),
                    TypeCode = (int)r.Type,
                    TypeKey = r.Type.ToString(),
                    Status = r.Status.ToString(),
                    r.Reason,
                    r.Description,
                    r.VideoUrl,
                    r.CreatedAt
                })
                .ToListAsync();

            return Success(new
            {
                PageNumber = page,
                PageSize = size,
                TotalItems = totalItems,
                Items = items
            });
        }

        public async Task<ResponseDTO> UpdateReportStatusAsync(Guid reportId, UpdateReportStatusDTO dto)
        {
            if (reportId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "ReportId không hợp lệ");

            if (dto == null)
                return Fail(BusinessCode.INVALID_INPUT, "Dữ liệu không hợp lệ");

            var report = await _reportRepo.GetById(reportId);

            if (report == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy report");

            report.Status = dto.Status;

            await _reportRepo.Update(report);
            await _uow.SaveChangeAsync();

            return Success(new
            {
                ReportId = report.Id,
                Status = report.Status.ToString(),
                report.UpdatedAt
            }, BusinessCode.UPDATE_SUCESSFULLY);
        }
    }
}