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
        Task<ResponseDTO> CreateReportAsync(Guid userId, Guid orderId, CreateReportDTO dto);
        Task<ResponseDTO> GetMyReportsAsync(Guid userId, int pageNumber, int pageSize);
        Task<ResponseDTO> GetReportDetailAsync(Guid userId, Guid reportId);
    }
}
