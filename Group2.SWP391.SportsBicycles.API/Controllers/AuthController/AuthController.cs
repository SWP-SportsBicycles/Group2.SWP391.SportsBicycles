using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.AuthController
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
        private string? Device() => Request.Headers.UserAgent.ToString();
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpDto dto)
        {
            var res = await _authService.SignUpAsync(dto);
            return Ok(res);
        }
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] SendOtpDto dto)
        {
            var res = await _authService.ResendOtpAsync(dto.Email);
            return Ok(res);
        }
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyDto dto)
        {
            var (ok, msg) = await _authService.VerifyOtpAsync(dto);
            return Ok(new { success = ok, message = msg });
        }
        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] LoginDto dto)
        {
            var res = await _authService.SignInAsync(dto, Ip(), Device());

            if (res.Success)
            {
                Response.Cookies.Append("refreshToken", res.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });
            }

            return Ok(res);
        }
        [HttpPost("renew-token")]
        public async Task<IActionResult> RenewToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            var res = await _authService.RenewTokenAsync(refreshToken ?? "", Ip(), Device());

            if (res.Success)
            {
                Response.Cookies.Append("refreshToken", res.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });
            }

            return Ok(res);
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            var (success, message, errorType) = await _authService.LogoutAsync(refreshToken ?? "");

            Response.Cookies.Delete("refreshToken");

            return Ok(new
            {
                success,
                message,
                errorType
            });
        }
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            string? userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                                   ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _authService.ChangePasswordAsync(userId, dto);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }
        [AllowAnonymous]
        [HttpPost("reset-password-by-link")]
        public async Task<IActionResult> ResetPasswordByLink(
    [FromQuery] string token,
    [FromBody] ResetPasswordByLinkDto dto)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Token không được để trống." });

            var result = await _authService.ResetPasswordByLinkAsync(token, dto);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            string? userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                                   ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _authService.GetMeAsync(userId);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            return Ok(result);
        }
    }
}
