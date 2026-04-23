using Group2.SWP391.SportsBicycles.Services.Contract;
using Group2.SWP391.SportsBicycles.Services.Implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileAccountController : ControllerBase
    {
        private readonly IUserService _userService;

        public ProfileAccountController(IUserService userService)
        {
            _userService = userService;
        }

        private Guid GetUserId()
        {
            string? userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                                   ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Không xác định được người dùng");

            return userId;
        }
        [HttpPost("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userId = GetUserId();

            var result = await _userService.UploadAvatarAsync(userId, file);

            return StatusCode(result.IsSucess ? 200 : 400, result);
        }

    }
}
