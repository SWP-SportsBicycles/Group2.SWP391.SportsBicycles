using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder);
        Task<string> UploadVideoAsync(IFormFile file, string folder);
        Task<(bool IsSuccess, string Url, string Message)> UploadFileAsync(IFormFile file, string folder);
        Task<string> UploadAvatarAsync(Guid userId, IFormFile file);
        Task DeleteImageAsync(string publicId);

    }
}
