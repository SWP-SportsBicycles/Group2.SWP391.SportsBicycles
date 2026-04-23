using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Group2.SWP391.SportsBicycles.API.Controllers.BuyerController
{
    [ApiController]
    [Route("api/buyer-listing")]
    [Authorize(Roles = "BUYER")]

    public class BuyerListingController : ControllerBase
    {
        private readonly IBuyerListingService _service;

        public BuyerListingController(IBuyerListingService service)
        {
            _service = service;
        }

        // ================= LIST =================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetAllAsync(pageNumber, pageSize);
            return HandleResult(result);
        }

        // ================= DETAIL =================
        [HttpGet("{listingId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetail(Guid listingId)
        {
            var result = await _service.GetDetail(listingId);
            return HandleResult(result);
        }

        // ================= SEARCH =================
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(
            [FromQuery] string? keyword,
            [FromQuery] string? brand,
            [FromQuery] string? category,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? frameSize,
            [FromQuery] string? condition,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.SearchAsync(
                keyword,
                brand,
                category,
                minPrice,
                maxPrice,
                frameSize,
                condition,
                pageNumber,
                pageSize
            );

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
