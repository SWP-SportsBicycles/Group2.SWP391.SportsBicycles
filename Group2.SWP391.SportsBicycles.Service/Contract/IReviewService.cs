using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IReviewService
    {
        Task<ResponseDTO> CreateReviewAsync(Guid userId, CreateReviewDTO dto);
        Task<ResponseDTO> GetMyReviewsAsync(Guid sellerId);
        Task<ResponseDTO> GetMyReviewedOrdersAsync(Guid buyerId);

    }
}
