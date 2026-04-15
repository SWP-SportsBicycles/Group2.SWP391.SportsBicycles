using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IInspectorService
    {
        Task<ResponseDTO> GetPendingInspectionsAsync(int pageNumber, int pageSize);

        Task<ResponseDTO> GetInspectionDetailAsync(Guid orderId);

        // ================= SUBMIT =================
        Task<ResponseDTO> SubmitInspectionAsync(Guid inspectorId, Guid orderId, InspectionDTO dto);

        // ================= HISTORY =================
        Task<ResponseDTO> GetInspectionHistoryAsync(Guid inspectorId, int pageNumber, int pageSize);

        Task<ResponseDTO> GetInspectionHistoryDetailAsync(Guid inspectionId);
    }
}

