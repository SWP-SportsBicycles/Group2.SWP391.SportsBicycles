using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IAdminUserService
    {
        Task<ResponseDTO> GetUsersAsync(int page, int size, string? search, string? role, bool isDesc);
        Task<ResponseDTO> GetUserDetailAsync(Guid userId);
        Task<ResponseDTO> GetSellersAsync(int page, int size, string? search, bool isDesc);
        Task<ResponseDTO> GetBuyersAsync(int page, int size, string? search, bool isDesc);
        Task<ResponseDTO> BanUserAsync(Guid userId, BanUserDTO dto);
        Task<ResponseDTO> UnbanUserAsync(Guid userId);
    }
}
