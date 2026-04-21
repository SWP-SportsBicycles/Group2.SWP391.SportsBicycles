using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IInspectorListingService
    {
        Task<ResponseDTO> GetPendingListingsAsync(int pageNumber, int pageSize);
        Task<ResponseDTO> GetListingDetailAsync(Guid listingId);
        Task<ResponseDTO> SubmitToAdminAsync(Guid inspectorId, Guid listingId, ReviewListingDTO dto);
    }
}
