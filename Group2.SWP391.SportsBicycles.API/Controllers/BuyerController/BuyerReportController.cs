using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.BuyerController
{
    [ApiController]
    [Route("api/buyer-report")]
    [Authorize(Roles = "BUYER")]
    public class BuyerReportController : ControllerBase
    {
        private readonly IReportService _service;

        public BuyerReportController(IReportService service)
        {
            _service = service;
        }

        // ================= CREATE REPORT =================
        [HttpPost("{orderId}")]
        public async Task<IActionResult> CreateReport(Guid orderId, [FromBody] CreateReportDTO dto)
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

            var result = await _service.CreateReportAsync(buyerId, orderId, dto);
            return HandleResult(result);
        }

        // ================= GET MY REPORTS =================
        [HttpGet]
        public async Task<IActionResult> GetMyReports([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
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

            var result = await _service.GetMyReportsAsync(buyerId, pageNumber, pageSize);
            return HandleResult(result);
        }

        // ================= GET REPORT DETAIL =================
        [HttpGet("{reportId}")]
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
                User.FindFirst("userId")?.Value ??
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        // ================= HANDLE RESULT =================
        private IActionResult HandleResult(ResponseDTO result)
        {
            if (result == null)
            {
                return StatusCode(500, new ResponseDTO
                {
                    IsSucess = false,
                    BusinessCode = BusinessCode.INTERNAL_ERROR,
                    Message = "Lỗi hệ thống"
                });
            }

            if (!result.IsSucess)
            {
                return result.BusinessCode switch
                {
                    BusinessCode.DATA_NOT_FOUND => NotFound(result),

                    BusinessCode.VALIDATION_FAILED
                        or BusinessCode.VALIDATION_ERROR
                        or BusinessCode.INVALID_INPUT
                        or BusinessCode.INVALID_DATA
                        or BusinessCode.INVALID_ACTION => BadRequest(result),

                    BusinessCode.AUTH_NOT_FOUND
                        or BusinessCode.WRONG_PASSWORD => Unauthorized(result),

                    BusinessCode.ACCESS_DENIED
                        or BusinessCode.PERMISSION_DENIED => Forbid(),

                    BusinessCode.EXCEPTION
                        or BusinessCode.INTERNAL_ERROR => StatusCode(500, result),

                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
    }
}
