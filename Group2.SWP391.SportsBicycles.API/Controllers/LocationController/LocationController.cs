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
    public class LocationController : ControllerBase
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

        // ================= HANDLE RESULT =================
        private IActionResult HandleResult(ResponseDTO result)
        {
            if (result == null)
            {
                return StatusCode(500, new ResponseDTO
                {
                    IsSucess = false,
                    BusinessCode = BusinessCode.INTERNAL_ERROR,
                    Message = "Lỗi hệ thống"
                });
            }

            if (!result.IsSucess)
            {
                return result.BusinessCode switch
                {
                    BusinessCode.DATA_NOT_FOUND => NotFound(result),

                    BusinessCode.VALIDATION_FAILED
                        or BusinessCode.VALIDATION_ERROR
                        or BusinessCode.INVALID_INPUT
                        or BusinessCode.INVALID_DATA
                        or BusinessCode.INVALID_ACTION => BadRequest(result),

                    BusinessCode.AUTH_NOT_FOUND
                        or BusinessCode.WRONG_PASSWORD => Unauthorized(result),

                    BusinessCode.ACCESS_DENIED
                        or BusinessCode.PERMISSION_DENIED => Forbid(),

                    BusinessCode.EXCEPTION
                        or BusinessCode.INTERNAL_ERROR => StatusCode(500, result),

                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
    }
}
