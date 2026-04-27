using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Group2.SWP391.SportsBicycles.API.Controllers.AdminController
{
    [Route("api/[controller]")]
   // [Authorize(Roles = nameof(RoleEnum.ADMIN))]
    [ApiController]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _service;

        public AdminDashboardController(IAdminDashboardService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await _service.GetDashboardAsync();
            return Ok(result);
        }
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue([FromQuery] int months = 6)
        {
            var result = await _service.GetRevenueAnalyticsAsync(months);
            return Ok(result);
        }
        [HttpGet("listings")]
        public async Task<IActionResult> GetListings()
        {
            var result = await _service.GetListingAnalyticsAsync();
            return Ok(result);
        }
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var result = await _service.GetUserAnalyticsAsync();
            return Ok(result);
        }
    }
}
