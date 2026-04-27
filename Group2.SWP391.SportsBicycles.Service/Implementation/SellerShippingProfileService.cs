using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Helpers;
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
            ResponseDTO res = new ResponseDTO();

            try
            {
                if (dto == null)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_DATA;
                    res.Message = "Data null";
                    return res;
                }

                if (string.IsNullOrWhiteSpace(dto.SenderName) ||
                    string.IsNullOrWhiteSpace(dto.SenderPhone) ||
                    string.IsNullOrWhiteSpace(dto.SenderAddress))
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_DATA;
                    res.Message = "Thiếu thông tin người gửi";
                    return res;
                }

                if (dto.FromDistrictId <= 0 || string.IsNullOrWhiteSpace(dto.FromWardCode))
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_DATA;
                    res.Message = "Thiếu thông tin địa chỉ";
                    return res;
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.SenderPhone, @"^0\d{9}$"))
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_DATA;
                    res.Message = "Số điện thoại không hợp lệ";
                    return res;
                }

                if (string.IsNullOrWhiteSpace(dto.BankName) ||
                    string.IsNullOrWhiteSpace(dto.BankAccountNumber) ||
                    string.IsNullOrWhiteSpace(dto.BankAccountName))
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_DATA;
                    res.Message = "Thiếu thông tin tài khoản ngân hàng";
                    return res;
                }

                await _uow.BeginTransactionAsync();

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
                        BankName = dto.BankName.Trim(),
                        BankAccountNumber = dto.BankAccountNumber.Trim(),
                        BankAccountName = dto.BankAccountName.Trim(),
                        CreatedAt = DateTimeHelper.NowVN()
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
                    profile.FromWardName = dto.FromWardName?.Trim();
                    profile.FromDistrictName = dto.FromDistrictName?.Trim();
                    profile.FromProvinceName = dto.FromProvinceName?.Trim();
                    profile.BankName = dto.BankName.Trim();
                    profile.BankAccountNumber = dto.BankAccountNumber.Trim();
                    profile.BankAccountName = dto.BankAccountName.Trim();
                    profile.UpdatedAt = DateTimeHelper.NowVN();
                }

                await _uow.SaveChangeAsync();
                await _uow.CommitAsync();

                // ✅ response sạch
                res.IsSucess = true;
                res.BusinessCode = BusinessCode.UPDATE_SUCESSFULLY;
                res.Message = "Lưu profile thành công";
                res.Data = new
                {
                    profile.Id,
                    profile.SenderName,
                    profile.SenderPhone,
                    profile.SenderAddress,
                    profile.FromDistrictId,
                    profile.FromWardCode,
                    profile.BankName,
                    profile.BankAccountNumber,
                    profile.BankAccountName,
                    profile.IsDefault
                };
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();

                res.IsSucess = false;
                res.BusinessCode = BusinessCode.EXCEPTION;
                res.Message = "Lỗi: " + ex.Message;
            }

            return res;
        }

        public async Task<ResponseDTO> GetMyProfileAsync(Guid userId)
        {
            ResponseDTO res = new ResponseDTO();

            try
            {
                var profile = await _repo.AsQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

                if (profile == null)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.DATA_NOT_FOUND;
                    res.Message = "Chưa có profile";
                    return res;
                }

                res.IsSucess = true;
                res.BusinessCode = BusinessCode.GET_DATA_SUCCESSFULLY;
                res.Message = "Lấy profile thành công";
                res.Data = new
                {
                    profile.Id,
                    profile.SenderName,
                    profile.SenderPhone,
                    profile.SenderAddress,
                    profile.FromDistrictId,
                    profile.FromWardCode,
                    FromWardName = profile.FromWardName ?? "",
                    FromDistrictName = profile.FromDistrictName ?? "",
                    FromProvinceName = profile.FromProvinceName ?? "",
                    profile.BankName,
                    profile.BankAccountNumber,
                    profile.BankAccountName,
                    profile.IsDefault
                };
            }
            catch (Exception ex)
            {
                res.IsSucess = false;
                res.BusinessCode = BusinessCode.EXCEPTION;
                res.Message = "Lỗi: " + ex.Message;
            }

            return res;
        }
    }
}
