using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface ISellerMediaService
    {
        //Task<ResponseDTO> UploadAsync(Guid sellerId, Guid listingId, IFormFile file, MediaType type);
        Task<ResponseDTO> DeleteAsync(Guid sellerId, Guid mediaId);
        Task<ResponseDTO> UpdateTypeAsync(Guid sellerId, Guid mediaId, MediaType type);
        Task<ResponseDTO> UploadMultipleAsync(Guid sellerId, Guid listingId, List<IFormFile> files);
    }
}
