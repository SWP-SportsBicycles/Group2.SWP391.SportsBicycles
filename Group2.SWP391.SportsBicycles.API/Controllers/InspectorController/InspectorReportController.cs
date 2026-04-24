using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Group2.SWP391.SportsBicycles.API.Controllers.InspectorController
{
    [ApiController]
    [Route("api/inspector-report")]
    [Authorize(Roles = "INSPECTOR")]
    public class InspectorReportController : ControllerBase
    {
        private readonly IReportService _service;

        public InspectorReportController(IReportService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetReports(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? type = null)
        {
            var result = await _service.GetReportsForInspectorAsync(page, size, status, type);
            return Ok(result);
        }

        [HttpPut("{reportId:guid}/submit")]
        public async Task<IActionResult> SubmitToAdmin(Guid reportId)
        {
            var result = await _service.SubmitReportToAdminAsync(reportId);
            return Ok(result);
        }
    }
}
