using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface ISellerShippingProfileService
    {
        Task<ResponseDTO> UpsertAsync(Guid userId, SellerShippingProfileDTO dto);
        Task<ResponseDTO> GetMyProfileAsync(Guid userId);
    }
}
