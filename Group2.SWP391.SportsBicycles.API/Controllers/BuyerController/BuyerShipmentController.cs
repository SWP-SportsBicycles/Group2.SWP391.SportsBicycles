using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Group2.SWP391.SportsBicycles.API.Controllers.BuyerController
{
    [ApiController]
    [Route("api/buyer-shipment")]
    public class BuyerShipmentController : ControllerBase
    {
        private readonly IShipmentService _service;

        public BuyerShipmentController(IShipmentService service)
        {
            _service = service;
        }

        // ================= CREATE SHIPMENT =================
        [HttpPost("{orderId}")]
        public async Task<IActionResult> CreateShipment(Guid orderId, [FromBody] CreateShipmentDTO dto)
        {
            var result = await _service.CreateShipmentAsync(orderId, dto);
            return HandleResult(result);
        }

        // ================= GET SHIPMENT BY ORDER =================
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetShipmentByOrderId(Guid orderId)
        {
            var result = await _service.GetShipmentByOrderIdAsync(orderId);
            return HandleResult(result);
        }

        // ================= SYNC TRACKING =================
        [HttpPost("sync/{orderId}")]
        public async Task<IActionResult> SyncTracking(Guid orderId)
        {
            var result = await _service.SyncTrackingAsync(orderId);
            return HandleResult(result);
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
