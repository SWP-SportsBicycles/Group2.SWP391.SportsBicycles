using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IWishlistService
    {
        Task<ResponseDTO> AddToWishlistAsync(Guid bikeId);

        Task<ResponseDTO> RemoveFromWishlistAsync(Guid bikeId);

        Task<ResponseDTO> GetMyWishlistAsync(int pageNumber, int pageSize);
    }
}
