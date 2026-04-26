using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.BuyerController
{
    [ApiController]
    [Route("api/buyer-report")]
    [Authorize(Roles = "BUYER")]
    public class BuyerReportController : BaseController
    {
        private readonly IReportService _service;

        public BuyerReportController(IReportService service)
        {
            _service = service;
        }

        // ================= CREATE REPORT =================
        [HttpPost("{orderId}")]
        public async Task<IActionResult> CreateReport(
      Guid orderId,
      [FromForm] CreateReportDTO dto)
        {
            var buyerId = GetCurrentUserId();

            if (buyerId == Guid.Empty)
                return Unauthorized();

            var result = await _service.CreateReportAsync(buyerId, orderId, dto);
            return HandleResult(result);
        }

        // ================= GET MY REPORTS =================
        [HttpGet("my")]
        public async Task<IActionResult> GetMyReports()
        {
            var buyerId = GetCurrentUserId();
            if (buyerId == Guid.Empty)
            {
                return Unauthorized(new ResponseDTO
                {
                    IsSucess = false,
                    BusinessCode = BusinessCode.AUTH_NOT_FOUND,
                    Message = "Không xác định được người dùng"
                });
            }

            var result = await _service.GetMyReportsAsync(buyerId);
            return HandleResult(result);
        }


        // ================= GET REPORT DETAIL =================
        [HttpGet("{reportId:guid}")]
        public async Task<IActionResult> GetReportDetail(Guid reportId)
        {
            var buyerId = GetCurrentUserId();
            if (buyerId == Guid.Empty)
            {
                return Unauthorized(new ResponseDTO
                {
                    IsSucess = false,
                    BusinessCode = BusinessCode.AUTH_NOT_FOUND,
                    Message = "Không xác định được người dùng"
                });
            }

            var result = await _service.GetReportDetailAsync(buyerId, reportId);
            return HandleResult(result);
        }


      

        // ================= GET CURRENT USER ID =================
        private Guid GetCurrentUserId()
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("userId")?.Value ??
                User.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

    }
}