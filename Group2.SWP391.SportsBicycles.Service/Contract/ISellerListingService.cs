using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface ISellerListingService
    {
        // ================= CREATE =================
        Task<ResponseDTO> CreateAsync(Guid sellerId, ListingCreateDTO dto);

        // ================= SUBMIT =================
        Task<ResponseDTO> SubmitForReviewAsync(Guid sellerId, Guid listingId);

        // ================= UPDATE =================
        Task<ResponseDTO> UpdateAsync(Guid sellerId, Guid listingId, ListingUpsertDTO dto);

        // ================= DELETE =================
        Task<ResponseDTO> DeleteAsync(Guid sellerId, Guid listingId);

        // ================= GET =================
        Task<ResponseDTO> GetMyListingsAsync(Guid sellerId, int pageNumber, int pageSize);

        Task<ResponseDTO> GetDetailsAsync(Guid sellerId, Guid listingId);

        // ================= WITHDRAW =================
        Task<ResponseDTO> WithdrawAsync(Guid sellerId, Guid listingId);
    }
}
