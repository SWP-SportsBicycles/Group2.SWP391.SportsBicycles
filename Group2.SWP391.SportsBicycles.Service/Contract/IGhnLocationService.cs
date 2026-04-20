using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IGhnLocationService
    {
        Task<ResponseDTO> GetSupportedProvincesAsync();
        Task<ResponseDTO> GetDistrictsAsync(int provinceId);
        Task<ResponseDTO> GetWardsAsync(int districtId);
    }
}
