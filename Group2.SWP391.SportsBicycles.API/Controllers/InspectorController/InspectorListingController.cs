using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.InspectorController
{
    [ApiController]
    [Route("api/inspector-listing")]
    [Authorize(Roles = "INSPECTOR")]
    public class InspectorListingController : ControllerBase
    {
        private readonly IInspectorListingService _service;

        public InspectorListingController(IInspectorListingService service)
        {
            _service = service;
        }

        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;

            var userIdClaim = User.Claims.FirstOrDefault(c =>
                c.Type == JwtRegisteredClaimNames.Sub ||
                c.Type == ClaimTypes.NameIdentifier ||
                c.Type.EndsWith("/nameidentifier"));

            if (userIdClaim == null)
                return false;

            return Guid.TryParse(userIdClaim.Value, out userId);
        }

        private IActionResult UnauthorizedUser()
        {
            return Unauthorized(new ResponseDTO
            {
                IsSucess = false,
                BusinessCode = BusinessCode.AUTH_NOT_FOUND,
                Message = "Không xác định được người dùng"
            });
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingListings(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPendingListingsAsync(pageNumber, pageSize);
            return HandleResult(result);
        }

        [HttpGet("{listingId}")]
        public async Task<IActionResult> GetListingDetail([FromRoute] Guid listingId)
        {
            var result = await _service.GetListingDetailAsync(listingId);
            return HandleResult(result);
        }

        [HttpPost("{listingId}/submit-to-admin")]
        public async Task<IActionResult> SubmitToAdmin([FromRoute] Guid listingId, [FromBody] ReviewListingDTO dto)
        {
            if (!TryGetUserId(out var inspectorId))
                return UnauthorizedUser();

            var result = await _service.SubmitToAdminAsync(inspectorId, listingId, dto);
            return HandleResult(result);
        }

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

                    BusinessCode.CREATED_SUCCESSFULLY => StatusCode(StatusCodes.Status201Created, result),

                    _ => Ok(result)
                };
            }

            return Ok(result);
        }
    }
}