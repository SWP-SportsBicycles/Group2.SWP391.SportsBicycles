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
    public class InspectorController : ControllerBase
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
        public async Task<IActionResult> GetDetail(Guid orderId)
        {
            var result = await _service.GetInspectionDetailAsync(orderId);
            return HandleResult(result);
        }

        // ================= SUBMIT =================
        [HttpPost("{orderId}/submit")]
        public async Task<IActionResult> Submit(Guid orderId, [FromBody] InspectionDTO dto)
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
            var result = await _service.GetInspectionHistoryAsync(pageNumber, pageSize);
            return HandleResult(result);
        }

        [HttpGet("history/{inspectionId}")]
        public async Task<IActionResult> GetHistoryDetail(Guid inspectionId)
        {
            var result = await _service.GetInspectionHistoryDetailAsync(inspectionId);
            return HandleResult(result);
        }

        // ================= HANDLE =================
        private IActionResult HandleResult(ResponseDTO result)
        {
            if (result == null)
            {
                return StatusCode(500, new ResponseDTO
                {
                    IsSucess = false,
                    BusinessCode = BusinessCode.INTERNAL_ERROR
                });
            }

            if (!result.IsSucess)
            {
                return result.BusinessCode switch
                {
                    BusinessCode.DATA_NOT_FOUND => NotFound(result),

                    BusinessCode.INVALID_INPUT
                    or BusinessCode.INVALID_DATA
                    or BusinessCode.INVALID_ACTION => BadRequest(result),

                    BusinessCode.ACCESS_DENIED => Forbid(),

                    _ => StatusCode(500, result)
                };
            }

            return Ok(result);
        }
    }
}