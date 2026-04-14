using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task DeleteImageAsync(string publicId)
        {
            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));

            if (result.Result != "ok" && result.Result != "not found")
                throw new Exception("Xóa ảnh thất bại");
        }

        public async Task<string> UploadAvatarAsync(Guid userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "SportsBicycles/avatars",
                PublicId = $"user_{userId}",
                Overwrite = true,
                Transformation = new Transformation()
                    .Width(300)
                    .Height(300)
                    .Crop("fill")
                    .Gravity("face")
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(result.Error?.Message);

            return result.SecureUrl.ToString();
        }

        public async Task<(bool IsSuccess, string Url, string Message)> UploadFileAsync(IFormFile file, string folder)
        {
            try
            {
                string ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".bmp" || ext == ".webp")
                {
                    var imageParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, file.OpenReadStream()),
                        Folder = folder,
                        UseFilename = true,
                        UniqueFilename = true,
                        Overwrite = false,
                        Transformation = new Transformation()
                            .Quality("auto")
                            .FetchFormat("auto")
                    };

                    var result = await _cloudinary.UploadAsync(imageParams);

                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        return (true, result.SecureUrl.AbsoluteUri, "Upload thành công.");

                    return (false, "", result.Error?.Message ?? "Upload thất bại");
                }
                else
                {
                    var rawParams = new RawUploadParams
                    {
                        File = new FileDescription(file.FileName, file.OpenReadStream()),
                        Folder = folder,
                        UseFilename = true,
                        UniqueFilename = false,
                        Overwrite = true
                    };

                    var result = await _cloudinary.UploadAsync(rawParams);

                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        return (true, result.SecureUrl.AbsoluteUri, "Upload thành công.");

                    return (false, "", result.Error?.Message ?? "Upload thất bại");
                }
            }
            catch (Exception ex)
            {
                return (false, "", ex.Message);
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Không có file để upload.");

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                Transformation = new Transformation()
                    .Width(800)
                    .Height(800)
                    .Crop("limit")
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(uploadResult.Error?.Message);

            return uploadResult.SecureUrl.ToString();
        }
        

        public async Task<string> UploadVideoAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Không có file để upload.");

            await using var stream = file.OpenReadStream();

            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Upload video thất bại.");

            return uploadResult.SecureUrl.ToString();
        }
    }
}
