using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Group2.SWP391.SportsBicycles.Services.Implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.SellerController
{
    [ApiController]
    [Route("api/seller-listing")]
    [Authorize(Roles = nameof(RoleEnum.SELLER))]
    public class SellerListingController : ControllerBase
    {
        private readonly ISellerListingService _service;

        public SellerListingController(ISellerListingService service)
        {
            _service = service;
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User chưa đăng nhập");

            return Guid.Parse(userId);
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] ListingCreateDTO dto)
        {
            var result = await _service.CreateAsync(GetUserId(), dto);
            return HandleResult(result);
        }

        // ================= SUBMIT =================
        [HttpPost("{listingId}/submit")]
        public async Task<IActionResult> Submit([FromRoute] Guid listingId)
        {
            var result = await _service.SubmitForReviewAsync(GetUserId(), listingId);
            return HandleResult(result);
        }

        // ================= UPDATE =================
        [HttpPut("{listingId}")]
        public async Task<IActionResult> Update([FromRoute] Guid listingId, [FromBody] ListingUpsertDTO dto)
        {
            var result = await _service.UpdateAsync(GetUserId(), listingId, dto);
            return HandleResult(result);
        }

        // ================= DELETE =================
        [HttpDelete("{listingId}")]
        public async Task<IActionResult> Delete([FromRoute] Guid listingId)
        {
            var result = await _service.DeleteAsync(GetUserId(), listingId);
            return HandleResult(result);
        }

        // ================= GET MY LIST =================
        [HttpGet]
        public async Task<IActionResult> GetMyListings(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetMyListingsAsync(GetUserId(), pageNumber, pageSize);
            return HandleResult(result);
        }

        // ================= GET DETAIL =================
        [HttpGet("{listingId}")]
        public async Task<IActionResult> GetDetail([FromRoute] Guid listingId)
        {
            var result = await _service.GetDetailsAsync(GetUserId(), listingId);
            return HandleResult(result);
        }

        // ================= WITHDRAW =================
        [HttpPost("{listingId}/withdraw")]
        public async Task<IActionResult> Withdraw([FromRoute] Guid listingId)
        {
            var result = await _service.WithdrawAsync(GetUserId(), listingId);
            return HandleResult(result);
        }
        [HttpPost("{id}/resubmit")]
        public async Task<IActionResult> Resubmit(Guid id)
        {
            var userId = GetUserId(); // giữ logic auth của mày

            var result = await _service.ResubmitAsync(id, userId);

            return Ok(result);
        }

        // ================= HANDLE =================
        private IActionResult HandleResult(ResponseDTO result)
        {
            if (result == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDTO
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

                    BusinessCode.INVALID_INPUT
                    or BusinessCode.INVALID_DATA
                    or BusinessCode.INVALID_ACTION
                    or BusinessCode.VALIDATION_ERROR
                    or BusinessCode.VALIDATION_FAILED => BadRequest(result),

                    BusinessCode.AUTH_NOT_FOUND
                    or BusinessCode.WRONG_PASSWORD => Unauthorized(result),

                    BusinessCode.ACCESS_DENIED
                    or BusinessCode.PERMISSION_DENIED => StatusCode(StatusCodes.Status403Forbidden, result),

                    BusinessCode.EXCEPTION
                    or BusinessCode.INTERNAL_ERROR => StatusCode(StatusCodes.Status500InternalServerError, result),

                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
    }
}
