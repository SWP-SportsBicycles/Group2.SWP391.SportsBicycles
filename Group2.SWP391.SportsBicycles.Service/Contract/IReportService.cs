using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IReportService
    {
        // BUYER
        Task<ResponseDTO> CreateReportAsync(Guid buyerId, Guid orderId, CreateReportDTO dto);
        Task<ResponseDTO> GetMyReportsAsync(Guid buyerId);
        Task<ResponseDTO> GetReportDetailAsync(Guid buyerId, Guid reportId);

        // INSPECTOR
        Task<ResponseDTO> GetReportsForInspectorAsync(int page, int size, string? status, string? type);
        Task<ResponseDTO> InspectorConfirmReportAsync(Guid reportId);
        Task<ResponseDTO> InspectorRejectReportAsync(Guid reportId);


        // ADMIN
        Task<ResponseDTO> GetReportsForAdminAsync(int page, int size, string? status, string? type);
        //Task<ResponseDTO> ApproveReportAsync(Guid reportId);
        //Task<ResponseDTO> RejectReportAsync(Guid reportId);
        Task<ResponseDTO> ConfirmRefundAsync(Guid reportId);
        Task<ResponseDTO> GetReportDetailForAdminAsync(Guid reportId);

    }
}
