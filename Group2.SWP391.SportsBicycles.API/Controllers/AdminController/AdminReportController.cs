using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Group2.SWP391.SportsBicycles.API.Controllers.AdminController
{
    [ApiController]
    [Route("api/admin-report")]
    [Authorize(Roles = "ADMIN")]
    public class AdminReportController : ControllerBase
    {
        private readonly IReportService _service;

        public AdminReportController(IReportService service)
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
            var result = await _service.GetReportsForAdminAsync(page, size, status, type);
            return Ok(result);
        }

        [HttpPut("{reportId:guid}/approve")]
        public async Task<IActionResult> ApproveReport(Guid reportId)
        {
            var result = await _service.ApproveReportAsync(reportId);
            return Ok(result);
        }

        [HttpPut("{reportId:guid}/reject")]
        public async Task<IActionResult> RejectReport(Guid reportId)
        {
            var result = await _service.RejectReportAsync(reportId);
            return Ok(result);
        }
        [HttpPut("{reportId:guid}/refund")]
        public async Task<IActionResult> ConfirmRefund(Guid reportId)
        {
            var result = await _service.ConfirmRefundAsync(reportId);
            return Ok(result);
        }
    }
}

