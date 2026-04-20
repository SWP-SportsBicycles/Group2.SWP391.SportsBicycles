using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IAdminListingService
    {
        Task<ResponseDTO> GetListingsAsync(int page, int size, string? search, string? sortBy, bool isDesc);
        Task<ResponseDTO> GetDetailAsync(Guid listingId);
        Task<ResponseDTO> ApproveListingAsync(Guid listingId);
        Task<ResponseDTO> RejectListingAsync(Guid listingId, RejectListingDTO rejectDto);
    }
}
