using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class SellerShippingProfileService : ISellerShippingProfileService
    {
        private readonly IGenericRepository<SellerShippingProfile> _repo;
        private readonly IUnitOfWork _uow;

        public SellerShippingProfileService(
            IGenericRepository<SellerShippingProfile> repo,
            IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }
        private static ResponseDTO Success(object? data = null)
            => new() { IsSucess = true, BusinessCode = BusinessCode.GET_DATA_SUCCESSFULLY, Data = data };

        private static ResponseDTO Fail(BusinessCode code, string msg)
            => new() { IsSucess = false, BusinessCode = code, Message = msg };

        public async Task<ResponseDTO> UpsertAsync(Guid userId, SellerShippingProfileDTO dto)
        {
            if (dto == null)
                return Fail(BusinessCode.INVALID_DATA, "Data null");

            if (string.IsNullOrWhiteSpace(dto.SenderName) ||
                string.IsNullOrWhiteSpace(dto.SenderPhone) ||
                string.IsNullOrWhiteSpace(dto.SenderAddress))
            {
                return Fail(BusinessCode.INVALID_DATA, "Thiếu thông tin người gửi");
            }

            var profile = await _repo.AsQueryable()
                .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

            if (profile == null)
            {
                profile = new SellerShippingProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    SenderName = dto.SenderName.Trim(),
                    SenderPhone = dto.SenderPhone.Trim(),
                    SenderAddress = dto.SenderAddress.Trim(),
                    FromDistrictId = dto.FromDistrictId,
                    FromWardCode = dto.FromWardCode,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.Insert(profile);
            }
            else
            {
                profile.SenderName = dto.SenderName.Trim();
                profile.SenderPhone = dto.SenderPhone.Trim();
                profile.SenderAddress = dto.SenderAddress.Trim();
                profile.FromDistrictId = dto.FromDistrictId;
                profile.FromWardCode = dto.FromWardCode;
                profile.UpdatedAt = DateTime.UtcNow;
            }

            await _uow.SaveChangeAsync();

            return Success();
        }

        public async Task<ResponseDTO> GetMyProfileAsync(Guid userId)
        {
            var profile = await _repo.AsQueryable()
               .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

            if (profile == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Chưa có profile");

            return Success(profile);
        }
    }
}
