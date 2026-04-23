using Group2.SWP391.SportsBicycles.Common.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IUserService
    {
        Task<ResponseDTO> UploadAvatarAsync(Guid userId, IFormFile file);
    }
}
