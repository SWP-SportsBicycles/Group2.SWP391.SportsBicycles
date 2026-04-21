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
        Task<ResponseDTO> CreateReportAsync(Guid buyerId, Guid orderId, CreateReportDTO dto);
        Task<ResponseDTO> GetMyReportsAsync(Guid buyerId);
        Task<ResponseDTO> GetReportDetailAsync(Guid buyerId, Guid reportId);
        Task<ResponseDTO> GetReportsForAdminAsync(int page, int size, string? status, string? type);
        Task<ResponseDTO> UpdateReportStatusAsync(Guid reportId, UpdateReportStatusDTO dto);

    }
}
