using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace Group2.SWP391.SportsBicycles.API.Controllers.PaymentController
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;

            var userIdClaim = User.Claims.FirstOrDefault(c =>
                c.Type == JwtRegisteredClaimNames.Sub ||
                c.Type == ClaimTypes.NameIdentifier ||
                c.Type.EndsWith("/nameidentifier"));

            if (userIdClaim == null)
                return false;

            return Guid.TryParse(userIdClaim.Value, out userId);
        }

        private IActionResult UnauthorizedUser()
        {
            return Unauthorized(new ResponseDTO
            {
                IsSucess = false,
                BusinessCode = BusinessCode.AUTH_NOT_FOUND,
                Message = "Không xác định được người dùng"
            });
        }

        [Authorize(Roles = "BUYER")]
        [HttpPost("{orderId}")]
        public async Task<IActionResult> CreatePayment(Guid orderId)
        {
            if (!TryGetUserId(out var buyerId))
                return UnauthorizedUser();

            var result = await _paymentService.CreatePaymentLink(buyerId, orderId);
            return HandleResult(result);
        }

        [Authorize(Roles = "BUYER")]
        [HttpPost("cancel/{orderId}")]
        public async Task<IActionResult> CancelOrder(Guid orderId, [FromBody] CancelOrderDTO dto)
        {
            if (!TryGetUserId(out var buyerId))
                return UnauthorizedUser();

            var result = await _paymentService.CancelOrderAsync(buyerId, orderId, dto?.Reason);
            return HandleResult(result);
        }

        [Authorize(Roles = "BUYER")]
        [HttpPost("refund-info/{orderId}")]
        public async Task<IActionResult> SubmitRefundInfo(Guid orderId, [FromBody] RefundInfoDTO dto)
        {
            if (!TryGetUserId(out var buyerId))
                return UnauthorizedUser();

            var result = await _paymentService.SubmitRefundInfoAsync(buyerId, orderId, dto);
            return HandleResult(result);
        }

        [Authorize(Roles = "BUYER")]
        [HttpGet("refund-status/{orderId}")]
        public async Task<IActionResult> GetRefundStatus(Guid orderId)
        {
            if (!TryGetUserId(out var buyerId))
                return UnauthorizedUser();

            var result = await _paymentService.GetRefundStatusAsync(buyerId, orderId);
            return HandleResult(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("refund-confirm/{orderId}")]
        public async Task<IActionResult> ConfirmRefund(Guid orderId)
        {
            var result = await _paymentService.ConfirmRefundAsync(orderId);
            return HandleResult(result);
        }

        [AllowAnonymous]
        [HttpGet("/payment-success")]
        public IActionResult PaymentSuccess(
            [FromQuery] string? code,
            [FromQuery] string? id,
            [FromQuery] bool? cancel,
            [FromQuery] string? status,
            [FromQuery] long? orderCode)
        {
            return Ok(new
            {
                message = "Thanh toán thành công",
                code,
                id,
                cancel,
                status,
                orderCode
            });
        }

        [AllowAnonymous]
        [HttpGet("/payment-cancel")]
        public IActionResult PaymentCancel(
            [FromQuery] string? code,
            [FromQuery] string? id,
            [FromQuery] bool? cancel,
            [FromQuery] string? status,
            [FromQuery] long? orderCode)
        {
            return Ok(new
            {
                message = "Thanh toán bị hủy",
                code,
                id,
                cancel,
                status,
                orderCode
            });
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] JsonElement payload)
        {
            if (!payload.TryGetProperty("data", out var data))
                return BadRequest("Thiếu data");

            if (!data.TryGetProperty("orderCode", out var orderCodeElement))
                return BadRequest("Thiếu orderCode");

            string orderCode = orderCodeElement.ValueKind switch
            {
                JsonValueKind.Number => orderCodeElement.GetInt64().ToString(),
                JsonValueKind.String => orderCodeElement.GetString() ?? string.Empty,
                _ => string.Empty
            };

            var result = await _paymentService.HandlePaymentSuccessAsync(orderCode);

            return HandleResult(result);
        }

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

            return result.BusinessCode switch
            {
                BusinessCode.CREATED_SUCCESSFULLY => StatusCode(201, result),
                _ => Ok(result)
            };
        }
    }
}