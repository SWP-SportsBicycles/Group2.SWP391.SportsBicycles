using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Group2.SWP391.SportsBicycles.API.Controllers.LocationController
{
    [ApiController]
    [Route("api/location")]
    [AllowAnonymous]
    public class LocationController : BaseController
    {
        private readonly IGhnLocationService _service;

        public LocationController(IGhnLocationService service)
        {
            _service = service;
        }

        // ================= GET SUPPORTED PROVINCES =================
        [HttpGet("provinces")]
        public async Task<IActionResult> GetSupportedProvinces()
        {
            var result = await _service.GetSupportedProvincesAsync();
            return HandleResult(result);
        }

        // ================= GET DISTRICTS BY PROVINCE =================
        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts([FromQuery] int provinceId)
        {
            var result = await _service.GetDistrictsAsync(provinceId);
            return HandleResult(result);
        }

        // ================= GET WARDS BY DISTRICT =================
        [HttpGet("wards")]
        public async Task<IActionResult> GetWards([FromQuery] int districtId)
        {
            var result = await _service.GetWardsAsync(districtId);
            return HandleResult(result);
        }

    }
}
