using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IBuyerListingService
    {
        // ================= LIST =================
        Task<ResponseDTO> GetAllAsync(int pageNumber, int pageSize);

        // ================= DETAIL =================
        Task<ResponseDTO> GetDetail(Guid listingId);

        // ================= SEARCH =================
        Task<ResponseDTO> SearchAsync(
      string? keyword,
      string? brand,
      string? category,
      decimal? minPrice,
      decimal? maxPrice,
      string? frameSize,
      string? condition,
      string? city,
      int pageNumber,
      int pageSize);
    }
}

