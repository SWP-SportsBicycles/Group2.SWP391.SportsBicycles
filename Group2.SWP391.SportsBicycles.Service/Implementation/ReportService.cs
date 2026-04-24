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
        private readonly ICloudinaryService _cloud;
        private readonly IUnitOfWork _uow;

        public ReportService(
            IGenericRepository<Report> reportRepo,
            IGenericRepository<Order> orderRepo,
            IUnitOfWork uow,
            ICloudinaryService cloud)
        {
            _reportRepo = reportRepo;
            _orderRepo = orderRepo;
            _uow = uow;
            _cloud = cloud;
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

        private static string MapReportStatusDisplay(ReportStatusEnum status)
        {
            return status switch
            {
                ReportStatusEnum.Pending => "Đang chờ Inspector kiểm định",
                ReportStatusEnum.Reviewing => "Đang chờ Admin duyệt",
                ReportStatusEnum.Resolved => "Khiếu nại được chấp thuận - chờ hoàn tiền",
                ReportStatusEnum.Rejected => "Khiếu nại đã bị từ chối",
                _ => "Không xác định"
            };
        }

        private static string MapReportNextAction(ReportStatusEnum status)
        {
            return status switch
            {
                ReportStatusEnum.Pending => "Chờ Inspector kiểm tra report",
                ReportStatusEnum.Reviewing => "Chờ Admin duyệt hoặc từ chối",
                ReportStatusEnum.Resolved => "Buyer có thể nhập thông tin tài khoản hoàn tiền",
                ReportStatusEnum.Rejected => "Không thể hoàn tiền do report bị từ chối",
                _ => "Không xác định"
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

            string? videoUrl = null;

            if (dto.EvidenceVideo != null)
            {
                if (dto.EvidenceVideo.Length == 0)
                    return Fail(BusinessCode.INVALID_DATA, "Video bằng chứng bị rỗng");

                if (!dto.EvidenceVideo.ContentType.StartsWith("video"))
                    return Fail(BusinessCode.INVALID_DATA, "File bằng chứng phải là video");

                if (dto.EvidenceVideo.Length > 50 * 1024 * 1024)
                    return Fail(BusinessCode.INVALID_DATA, "Video bằng chứng tối đa 50MB");

                var ext = Path.GetExtension(dto.EvidenceVideo.FileName).ToLower();
                var allowedVideoExt = new[] { ".mp4", ".mov", ".avi" };

                if (!allowedVideoExt.Contains(ext))
                    return Fail(BusinessCode.INVALID_DATA, "Video chỉ hỗ trợ mp4, mov, avi");

                videoUrl = await _cloud.UploadVideoAsync(dto.EvidenceVideo, $"reports/{orderId}");
            }

            var report = new Report
            {
                Id = Guid.NewGuid(),
                Type = dto.Type,
                Reason = dto.Reason.Trim(),
                Description = dto.Description?.Trim(),
                VideoUrl = videoUrl,
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
                StatusDisplay = MapReportStatusDisplay(report.Status),
                NextAction = MapReportNextAction(report.Status),
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
                    StatusDisplay = MapReportStatusDisplay(r.Status),
                    NextAction = MapReportNextAction(r.Status),
                    r.Reason,
                    r.Description,
                    r.VideoUrl,
                    r.CreatedAt,
                    r.UpdatedAt
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
                    .ThenInclude(o => o.Transaction)
                .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == buyerId);

            if (report == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy report");

            return Success(new
            {
                ReportId = report.Id,
                report.OrderId,
                report.UserId,
                OrderStatus = report.Order.Status.ToString(),
                TransactionStatus = report.Order.Transaction == null
                    ? null
                    : report.Order.Transaction.Status.ToString(),
                Type = MapReportType(report.Type),
                TypeCode = (int)report.Type,
                TypeKey = report.Type.ToString(),
                Status = report.Status.ToString(),
                StatusDisplay = MapReportStatusDisplay(report.Status),
                NextAction = MapReportNextAction(report.Status),
                report.Reason,
                report.Description,
                report.VideoUrl,
                report.CreatedAt,
                report.UpdatedAt,
                CanSubmitRefundInfo =
                    report.Status == ReportStatusEnum.Resolved &&
                    report.Order.Transaction != null &&
                    report.Order.Transaction.Status == TransactionStatusEnum.RefundPending
            });
        }

        public async Task<ResponseDTO> GetReportsForInspectorAsync(int page, int size, string? status, string? type)
        {
            return await GetReportsInternalAsync(page, size, status, type);
        }

        public async Task<ResponseDTO> SubmitReportToAdminAsync(Guid reportId)
        {
            if (reportId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "ReportId không hợp lệ");

            var report = await _reportRepo.GetById(reportId);

            if (report == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy report");

            if (report.Status != ReportStatusEnum.Pending)
                return Fail(BusinessCode.INVALID_ACTION, "Chỉ được chuyển report đang Pending sang Admin review");

            report.Status = ReportStatusEnum.Reviewing;

            await _reportRepo.Update(report);
            await _uow.SaveChangeAsync();

            return Success(new
            {
                ReportId = report.Id,
                Status = report.Status.ToString(),
                StatusDisplay = MapReportStatusDisplay(report.Status),
                NextAction = MapReportNextAction(report.Status),
                Message = "Inspector đã kiểm tra và gửi report qua Admin duyệt",
                report.UpdatedAt
            }, BusinessCode.UPDATE_SUCESSFULLY);
        }

        public async Task<ResponseDTO> GetReportsForAdminAsync(int page, int size, string? status, string? type)
        {
            return await GetReportsInternalAsync(page, size, status, type);
        }

        public async Task<ResponseDTO> ApproveReportAsync(Guid reportId)
        {
            if (reportId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "ReportId không hợp lệ");

            var report = await _reportRepo.AsQueryable()
                .Include(r => r.Order)
                    .ThenInclude(o => o.Transaction)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy report");

            if (report.Status != ReportStatusEnum.Reviewing)
                return Fail(BusinessCode.INVALID_ACTION, "Chỉ được duyệt report đã được Inspector gửi qua Admin");

            if (report.Order == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy order của report");

            if (report.Order.Transaction == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Order chưa có transaction");

            if (report.Order.Transaction.Status != TransactionStatusEnum.Paid)
                return Fail(BusinessCode.INVALID_ACTION, "Transaction không hợp lệ để tạo hoàn tiền");

            report.Status = ReportStatusEnum.Resolved;
            report.Order.Transaction.Status = TransactionStatusEnum.RefundPending;
            report.Order.Transaction.Description =
                (report.Order.Transaction.Description ?? "") +
                " | Admin approved report, waiting buyer refund info";

            await _reportRepo.Update(report);
            await _uow.SaveChangeAsync();

            return Success(new
            {
                ReportId = report.Id,
                report.OrderId,
                ReportStatus = report.Status.ToString(),
                StatusDisplay = MapReportStatusDisplay(report.Status),
                NextAction = MapReportNextAction(report.Status),
                TransactionStatus = report.Order.Transaction.Status.ToString(),
                Message = "Admin đã chấp thuận khiếu nại. Buyer có thể nhập thông tin hoàn tiền.",
                report.UpdatedAt
            }, BusinessCode.UPDATE_SUCESSFULLY);
        }

        public async Task<ResponseDTO> RejectReportAsync(Guid reportId)
        {
            if (reportId == Guid.Empty)
                return Fail(BusinessCode.INVALID_INPUT, "ReportId không hợp lệ");

            var report = await _reportRepo.GetById(reportId);

            if (report == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy report");

            if (report.Status != ReportStatusEnum.Reviewing)
                return Fail(BusinessCode.INVALID_ACTION, "Chỉ được từ chối report đã được Inspector gửi qua Admin");

            report.Status = ReportStatusEnum.Rejected;

            await _reportRepo.Update(report);
            await _uow.SaveChangeAsync();

            return Success(new
            {
                ReportId = report.Id,
                ReportStatus = report.Status.ToString(),
                StatusDisplay = MapReportStatusDisplay(report.Status),
                NextAction = MapReportNextAction(report.Status),
                Message = "Admin đã từ chối khiếu nại",
                report.UpdatedAt
            }, BusinessCode.UPDATE_SUCESSFULLY);
        }

        private async Task<ResponseDTO> GetReportsInternalAsync(int page, int size, string? status, string? type)
        {
            if (page <= 0 || size <= 0)
                return Fail(BusinessCode.INVALID_INPUT, "Thông tin phân trang không hợp lệ");

            var query = _reportRepo.AsQueryable()
                .Include(r => r.Order)
                    .ThenInclude(o => o.Transaction)
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
                    OrderStatus = r.Order.Status.ToString(),
                    TransactionStatus = r.Order.Transaction == null
                        ? null
                        : r.Order.Transaction.Status.ToString(),
                    Type = MapReportType(r.Type),
                    TypeCode = (int)r.Type,
                    TypeKey = r.Type.ToString(),
                    Status = r.Status.ToString(),
                    StatusDisplay = MapReportStatusDisplay(r.Status),
                    NextAction = MapReportNextAction(r.Status),
                    r.Reason,
                    r.Description,
                    r.VideoUrl,
                    r.CreatedAt,
                    r.UpdatedAt
                })
                .ToListAsync();

            return Success(new
            {
                PageNumber = page,
                PageSize = size,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)size),
                Items = items
            });
        }
    }
}