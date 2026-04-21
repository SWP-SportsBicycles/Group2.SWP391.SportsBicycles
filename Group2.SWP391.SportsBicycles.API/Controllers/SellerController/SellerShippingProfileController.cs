using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.SellerController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = nameof(RoleEnum.SELLER))]
    public class SellerShippingProfileController : ControllerBase
    {
        private readonly ISellerShippingProfileService _service;

        public SellerShippingProfileController(ISellerShippingProfileService service)
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

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] SellerShippingProfileDTO dto)
        {
            var result = await _service.UpsertAsync(GetUserId(), dto);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyProfile()
        {
            var result = await _service.GetMyProfileAsync(GetUserId());
            return Ok(result);
        }
    }
}
