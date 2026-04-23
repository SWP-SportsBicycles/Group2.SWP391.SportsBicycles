using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Group2.SWP391.SportsBicycles.Services.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Group2.SWP391.SportsBicycles.API.Controllers.AdminController
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminUserController : ControllerBase
    {
        private readonly IAdminUserService _service;

        public AdminUserController(IAdminUserService service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> GetUsers(
          int page = 1,
          int size = 10,
          string? search = null,
          string? role = null,
          bool isDesc = false)
        {
            return Ok(await _service.GetUsersAsync(page, size, search, role, isDesc));
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetDetail(Guid userId)
        {
            return Ok(await _service.GetUserDetailAsync(userId));
        }
        [HttpGet("sellers")]
        public async Task<IActionResult> GetSellers(
      int page = 1,
      int size = 10,
      string? search = null,
      bool isDesc = false)
        {
            return Ok(await _service.GetSellersAsync(page, size, search, isDesc));
        }

        [HttpGet("buyers")]
        public async Task<IActionResult> GetBuyers(
            int page = 1,
            int size = 10,
            string? search = null,
            bool isDesc = false)
        {
            return Ok(await _service.GetBuyersAsync(page, size, search, isDesc));
        }
        [HttpPut("ban/{userId}")]
        public async Task<IActionResult> BanUser(Guid userId, [FromBody] BanUserDTO body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Reason))
                return BadRequest(new { Message = "Lý do không được để trống" });

            var result = await _service.BanUserAsync(userId, body);
            return StatusCode(result.IsSucess ? 200 : 400, result);
        }
        [HttpPut("unban/{userId}")]
        public async Task<IActionResult> UnbanUser(Guid userId)
        {
            var result = await _service.UnbanUserAsync(userId);
            return StatusCode(result.IsSucess ? 200 : 400, result);
        }
    }
}
