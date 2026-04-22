using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Group2.SWP391.SportsBicycles.Services.Implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Group2.SWP391.SportsBicycles.API.Controllers.AdminController
{
    [ApiController]
    [Route("api/admin-listing")]
    [Authorize(Roles = nameof(RoleEnum.ADMIN))]
    public class AdminListingController : ControllerBase
    {
        private readonly IAdminListingService _service;

        public AdminListingController(IAdminListingService service)
        {
            _service = service;
        }

        // ================= APPROVE =================
        [HttpPost("{listingId}/approve")]
        public async Task<IActionResult> Approve([FromRoute] Guid listingId)
        {
            var result = await _service.ApproveListingAsync(listingId);
            return HandleResult(result);
        }

        // ================= REJECT =================
        [HttpPost("{listingId}/reject")]
        public async Task<IActionResult> Reject([FromRoute] Guid listingId, [FromBody] RejectListingDTO dto)
        {
            var result = await _service.RejectListingAsync(listingId, dto);
            return HandleResult(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetList(
    int page = 1,
    int size = 10,
    string? search = null,
    string? sortBy = null,
    bool isDesc = false)
        {
            var result = await _service.GetListingsAsync(page, size, search, sortBy, isDesc);
            return HandleResult(result);
        }

        [HttpGet("{listingId}")]
        public async Task<IActionResult> GetDetail(Guid listingId)
        {
            var result = await _service.GetDetailAsync(listingId);
            return HandleResult(result);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll(int page = 1, int size = 10, string? status = null)
        {
            var result = await _service.GetAllListingsAsync(page, size, status);
            return Ok(result);
        }

        // ================= HANDLE =================
        private IActionResult HandleResult(ResponseDTO result)
        {
            if (result == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDTO
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

                    BusinessCode.INVALID_INPUT
                    or BusinessCode.INVALID_DATA
                    or BusinessCode.INVALID_ACTION
                    or BusinessCode.VALIDATION_ERROR
                    or BusinessCode.VALIDATION_FAILED => BadRequest(result),

                    BusinessCode.AUTH_NOT_FOUND
                    or BusinessCode.WRONG_PASSWORD => Unauthorized(result),

                    BusinessCode.ACCESS_DENIED
                    or BusinessCode.PERMISSION_DENIED => StatusCode(StatusCodes.Status403Forbidden, result),

                    BusinessCode.EXCEPTION
                    or BusinessCode.INTERNAL_ERROR => StatusCode(StatusCodes.Status500InternalServerError, result),

                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
    }
}