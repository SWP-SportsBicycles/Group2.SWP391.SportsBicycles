using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Helpers;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class UserService : IUserService
    {
        private readonly IGenericRepository<User> _userRepo;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IUnitOfWork _uow;

        public UserService(
            IGenericRepository<User> userRepo,
            ICloudinaryService cloudinaryService,
            IUnitOfWork uow)
        {
            _userRepo = userRepo;
            _cloudinaryService = cloudinaryService;
            _uow = uow;
        }

        public async Task<ResponseDTO> UploadAvatarAsync(Guid userId, IFormFile file)
        {
            var dto = new ResponseDTO();

            try
            {
                // ===== VALIDATE FILE =====
                if (file == null || file.Length == 0)
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.INVALID_DATA;
                    dto.Message = "File không hợp lệ.";
                    return dto;
                }

                // size <= 2MB
                if (file.Length > 2 * 1024 * 1024)
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.INVALID_DATA;
                    dto.Message = "Ảnh vượt quá 2MB.";
                    return dto;
                }

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowedExt.Contains(ext))
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.INVALID_DATA;
                    dto.Message = "Chỉ chấp nhận jpg, jpeg, png, webp.";
                    return dto;
                }

                // ===== CHECK USER =====
                var user = await _userRepo.GetFirstByExpression(x => x.Id == userId);

                if (user == null)
                {
                    dto.IsSucess = false;
                    dto.BusinessCode = BusinessCode.DATA_NOT_FOUND;
                    dto.Message = "Không tìm thấy user.";
                    return dto;
                }

                // ===== UPLOAD CLOUD =====
                var url = await _cloudinaryService.UploadAvatarAsync(userId, file);

                // ===== UPDATE DB =====
                user.AvtUrl = url;
                user.UpdatedAt = DateTimeHelper.NowVN();

                await _userRepo.Update(user);
                await _uow.SaveChangeAsync();

                // ===== RESPONSE =====
                dto.IsSucess = true;
                dto.BusinessCode = BusinessCode.UPDATE_SUCESSFULLY;
                dto.Message = "Upload avatar thành công.";
                dto.Data = new
                {
                    AvatarUrl = url
                };
            }
            catch (Exception ex)
            {
                dto.IsSucess = false;
                dto.BusinessCode = BusinessCode.EXCEPTION;
                dto.Message = "Lỗi upload avatar: " + ex.Message;
            }

            return dto;
        }
        }
    }
