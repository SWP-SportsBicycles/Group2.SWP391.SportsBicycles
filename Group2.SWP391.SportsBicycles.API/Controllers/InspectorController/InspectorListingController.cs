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
    public class InspectorListingController : BaseController
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

    }
}