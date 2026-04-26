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
    public class PaymentController : BaseController
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

        //[AllowAnonymous]
        //[HttpGet("/payment-success")]
        //public IActionResult PaymentSuccess(
        //    [FromQuery] string? code,
        //    [FromQuery] string? id,
        //    [FromQuery] bool? cancel,
        //    [FromQuery] string? status,
        //    [FromQuery] long? orderCode)
        //{
        //    return Ok(new
        //    {
        //        message = "Thanh toán thành công",
        //        code,
        //        id,
        //        cancel,
        //        status,
        //        orderCode
        //    });
        //}




        [AllowAnonymous]
        [HttpGet("payment-success")] 
        public async Task<IActionResult> PaymentSuccess(
    [FromQuery] long? orderCode)
        {
            if (orderCode != null)
            {
                await _paymentService.HandlePaymentSuccessAsync(orderCode.ToString());
            }

            return Ok(new
            {
                message = "Thanh toán thành công",
                orderCode
            });
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] JsonElement payload)
        {
            try
            {
                if (payload.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("orderCode", out var orderCodeElement))
                {
                    string orderCode = orderCodeElement.ValueKind switch
                    {
                        JsonValueKind.Number => orderCodeElement.GetInt64().ToString(),
                        JsonValueKind.String => orderCodeElement.GetString() ?? string.Empty,
                        _ => string.Empty
                    };

                    if (!string.IsNullOrEmpty(orderCode))
                    {
                        await _paymentService.HandlePaymentSuccessAsync(orderCode);
                    }
                }

                // 🔥 LUÔN trả 200 cho PayOS
                return Ok();
            }
            catch
            {
                return Ok(); // vẫn trả 200
            }
        }

    }
}