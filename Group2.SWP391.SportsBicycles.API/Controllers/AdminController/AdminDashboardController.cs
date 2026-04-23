using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Group2.SWP391.SportsBicycles.API.Controllers.AdminController
{
    [Route("api/[controller]")]
    [Authorize(Roles = nameof(RoleEnum.ADMIN))]
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
    }
}
