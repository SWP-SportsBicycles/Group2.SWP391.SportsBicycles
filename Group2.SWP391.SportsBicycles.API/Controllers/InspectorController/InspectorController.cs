using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.InspectorController
{
    [ApiController]
    [Route("api/inspector")]
    [Authorize]
    public class InspectorController : BaseController
    {
        private readonly IInspectorService _service;

        public InspectorController(IInspectorService service)
        {
            _service = service;
        }

        private Guid GetUserId()
        {
            return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // ================= PENDING =================
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPendingInspectionsAsync(pageNumber, pageSize);
            return HandleResult(result);
        }

        // ================= DETAIL =================
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetDetail([FromRoute] Guid orderId)
        {
            var result = await _service.GetInspectionDetailAsync(orderId);
            return HandleResult(result);
        }

        // ================= SUBMIT =================
        [HttpPost("{orderId}/submit")]
        public async Task<IActionResult> Submit([FromRoute] Guid orderId, [FromBody] InspectionDTO dto)
        {
            var result = await _service.SubmitInspectionAsync(GetUserId(), orderId, dto);
            return HandleResult(result);
        }

        // ================= HISTORY =================
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
      [FromQuery] int pageNumber = 1,
      [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetInspectionHistoryAsync(
                GetUserId(),
                pageNumber,
                pageSize);

            return HandleResult(result);
        }

        [HttpGet("history/{inspectionId}")]
        public async Task<IActionResult> GetHistoryDetail([FromRoute] Guid inspectionId)
        {
            var result = await _service.GetInspectionHistoryDetailAsync(inspectionId);
            return HandleResult(result);
        }

       
    }
}