using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.BuyerController
{
    [ApiController]
    [Route("api/buyer-order")]
    [Authorize]
    public class BuyerOrderController : ControllerBase
    {
        private readonly IBuyerOrderService _service;

        public BuyerOrderController(IBuyerOrderService service)
        {
            _service = service;
        }

        private Guid GetUserId()
        {
            return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // ================= CREATE ORDER =================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDTO dto)
        {
            var result = await _service.CreateOrderAsync(GetUserId(), dto);
            return HandleResult(result);
        }

        // ================= MARK PAID =================
        [HttpPost("{orderId}/paid")]
        public async Task<IActionResult> MarkPaid([FromRoute] Guid orderId)
        {
            var result = await _service.MarkOrderPaidAsync(orderId);
            return HandleResult(result);
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