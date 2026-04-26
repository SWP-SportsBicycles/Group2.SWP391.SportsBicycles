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
    public class InspectorReportController : BaseController
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
            return HandleResult(result);
        }

      


        [HttpPut("{reportId:guid}/confirm")]
        public async Task<IActionResult> ConfirmReport(Guid reportId)
        {
            var result = await _service.InspectorConfirmReportAsync(reportId);
            return HandleResult(result);
        }

        [HttpPut("{reportId:guid}/reject")]
        public async Task<IActionResult> RejectReport(Guid reportId)
        {
            var result = await _service.InspectorRejectReportAsync(reportId);
            return HandleResult(result);
        }
    }
}
