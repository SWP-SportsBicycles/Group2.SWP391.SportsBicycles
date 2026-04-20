using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class SellerMediaService : ISellerMediaService
    {
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IGenericRepository<Media> _mediaRepo;
        private readonly ICloudinaryService _cloud;
        private readonly IUnitOfWork _uow;

        public SellerMediaService(
            IGenericRepository<Listing> listingRepo,
            IGenericRepository<Bike> bikeRepo,
            IGenericRepository<Media> mediaRepo,
            ICloudinaryService cloud,
            IUnitOfWork uow)
        {
            _listingRepo = listingRepo;
            _bikeRepo = bikeRepo;
            _mediaRepo = mediaRepo;
            _cloud = cloud;
            _uow = uow;
        }

        private ResponseDTO Ok(object? d = null)
            => new() { IsSucess = true, Data = d };

        private ResponseDTO Fail(string msg)
            => new() { IsSucess = false, Message = msg };

        // ================= UPLOAD =================
        //public async Task<ResponseDTO> UploadAsync(Guid sellerId, Guid listingId, IFormFile file, MediaType type)
        //{
        //    var listing = await _listingRepo.AsQueryable()
        //        .Include(x => x.Bikes)
        //        .FirstOrDefaultAsync(x => x.Id == listingId && x.UserId == sellerId);

        //    if (listing == null)
        //        return Fail("Listing không tồn tại");

        //    var bike = listing.Bikes.FirstOrDefault();
        //    if (bike == null)
        //        return Fail("Bike không tồn tại");

        //    string url;

        //    if (file == null || file.Length == 0)
        //        return Fail("File rỗng");



        //    if (file.ContentType.StartsWith("image"))
        //    {
        //        url = await _cloud.UploadImageAsync(file, $"listing/{listingId}");
        //    }
        //    else if (file.ContentType.StartsWith("video"))
        //    {
        //        url = await _cloud.UploadVideoAsync(file, $"listing/{listingId}");
        //    }
        //    else
        //    {
        //        return Fail("File không hợp lệ");
        //    }

        //    var media = new Media
        //    {
        //        Id = Guid.NewGuid(),
        //        BikeId = bike.Id,
        //        Image = file.ContentType.StartsWith("image") ? url : null,
        //        VideoUrl = file.ContentType.StartsWith("video") ? url : null,
        //        Type = type
        //    };

        //    await _mediaRepo.Insert(media);

        //    if (listing.Status == ListingStatusEnum.Published)
        //    {
        //        listing.Status = ListingStatusEnum.PendingReview;
        //        bike.Status = BikeStatusEnum.PendingInspection;
        //    }

        //    await _uow.SaveChangeAsync();

        //    return Ok(new { url });
        //}

        // ================= DELETE =================
        public async Task<ResponseDTO> DeleteAsync(Guid sellerId, Guid mediaId)
        {
            var media = await _mediaRepo.GetById(mediaId);
            if (media == null)
                return Fail("Không tìm thấy media");

            var bike = await _bikeRepo.GetById(media.BikeId);
            if (bike == null)
                return Fail("Bike không tồn tại");

            var listing = await _listingRepo.GetById(bike.ListingId);
            if (listing == null || listing.UserId != sellerId)
                return Fail("Không có quyền");

            media.IsDeleted = true;

            if (listing.Status == ListingStatusEnum.Published)
            {
                listing.Status = ListingStatusEnum.PendingReview;
                bike.Status = BikeStatusEnum.PendingInspection;
            }

            await _uow.SaveChangeAsync();

            return Ok();
        }

        // ================= UPDATE TYPE =================
        public async Task<ResponseDTO> UpdateTypeAsync(Guid sellerId, Guid mediaId, MediaType type)
        {
            var media = await _mediaRepo.GetById(mediaId);
            if (media == null)
                return Fail("Không tìm thấy media");

            var bike = await _bikeRepo.GetById(media.BikeId);
            var listing = await _listingRepo.GetById(bike.ListingId);

            if (listing == null || listing.UserId != sellerId)
                return Fail("Không có quyền");

            media.Type = type;

            if (listing.Status == ListingStatusEnum.Published)
            {
                listing.Status = ListingStatusEnum.PendingReview;
                bike.Status = BikeStatusEnum.PendingInspection;
            }

            await _uow.SaveChangeAsync();

            return Ok();
        }

        public async Task<ResponseDTO> UploadMultipleAsync(Guid sellerId, Guid listingId, List<IFormFile> files)
        {
            // ===== CONFIG RULE =====
            const int MAX_IMAGES = 3;
            const int MAX_VIDEOS = 1;
            const int MAX_TOTAL_FILES = 4;
            const long MAX_IMAGE_SIZE = 5 * 1024 * 1024;   // 5MB
            const long MAX_VIDEO_SIZE = 50 * 1024 * 1024;  // 50MB

            var allowedImageExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tiff", ".jfif" };
            var allowedVideoExt = new[] { ".mp4", ".mov", ".avi" };
            // ===== VALIDATE INPUT =====
            if (files == null || !files.Any())
                return Fail("Không có file upload");

            if (files.Count > MAX_TOTAL_FILES)
                return Fail($"Tối đa {MAX_TOTAL_FILES} file (3 ảnh + 1 video)");

            if (files.Sum(f => f.Length) > 100 * 1024 * 1024)
                return Fail("Tổng dung lượng vượt quá 100MB");

            var listing = await _listingRepo.AsQueryable()
                .Include(x => x.Bikes)
                .FirstOrDefaultAsync(x => x.Id == listingId && x.UserId == sellerId);

            if (listing == null)
                return Fail("Listing không tồn tại");

            var bike = listing.Bikes.FirstOrDefault();
            if (bike == null)
                return Fail("Bike không tồn tại");

            // ===== EXISTING MEDIA =====
            var existingMedia = await _mediaRepo.AsQueryable()
                .Where(x => x.BikeId == bike.Id && !x.IsDeleted)
                .ToListAsync();

            int currentImages = existingMedia.Count(x => x.Image != null);
            int currentVideos = existingMedia.Count(x => x.VideoUrl != null);

            var urls = new List<string>();
            bool hasThumbnail = existingMedia.Any(x => x.Type == MediaType.Normal);

            // ===== LOOP =====
            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                    return Fail("File rỗng");

                var ext = Path.GetExtension(file.FileName).ToLower();

                // ===== IMAGE =====
                if (file.ContentType.StartsWith("image"))
                {
                    if (!allowedImageExt.Contains(ext))
                        return Fail("Ảnh không đúng định dạng");

                    if (file.Length > MAX_IMAGE_SIZE)
                        return Fail("Ảnh vượt quá 5MB");

                    if (currentImages >= MAX_IMAGES)
                        return Fail($"Chỉ tối đa {MAX_IMAGES} ảnh");

                    var url = await _cloud.UploadImageAsync(file, $"listing/{listingId}");
                    currentImages++;

                    var media = new Media
                    {
                        Id = Guid.NewGuid(),
                        BikeId = bike.Id,
                        Image = url,
                        VideoUrl = null,

                        // 👉 AUTO THUMBNAIL: ảnh đầu tiên
                        Type = hasThumbnail ? MediaType.Groupset : MediaType.Normal
                    };

                    hasThumbnail = true;

                    await _mediaRepo.Insert(media);
                    urls.Add(url);
                }

                // ===== VIDEO =====
                else if (file.ContentType.StartsWith("video"))
                {
                    if (!allowedVideoExt.Contains(ext))
                        return Fail("Video không đúng định dạng");

                    if (file.Length > MAX_VIDEO_SIZE)
                        return Fail("Video vượt quá 50MB");

                    if (currentVideos >= MAX_VIDEOS)
                        return Fail("Chỉ được 1 video");

                    var url = await _cloud.UploadVideoAsync(file, $"listing/{listingId}");
                    currentVideos++;

                    var media = new Media
                    {
                        Id = Guid.NewGuid(),
                        BikeId = bike.Id,
                        Image = null,
                        VideoUrl = url,
                        Type = MediaType.Normal
                    };

                    await _mediaRepo.Insert(media);
                    urls.Add(url);
                }
                else
                {
                    return Fail("File không hợp lệ");
                }
            }

            // ===== UPDATE STATUS =====
            if (listing.Status == ListingStatusEnum.Published)
            {
                listing.Status = ListingStatusEnum.PendingReview;
                bike.Status = BikeStatusEnum.PendingInspection;
            }

            await _uow.SaveChangeAsync();

            return Ok(new
            {
                Uploaded = urls.Count,
                Images = currentImages,
                Videos = currentVideos,
                Urls = urls
            });
        }
    }
}
