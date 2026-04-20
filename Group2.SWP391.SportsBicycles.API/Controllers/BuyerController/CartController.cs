using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.BuyerController
{
    [ApiController]
    [Route("api/buyer-cart")]
    [Authorize(Roles = "BUYER")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(userId) ? Guid.Empty : Guid.Parse(userId);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDTO dto)
        {
            var result = await _cartService.AddToCartAsync(GetUserId(), dto);
            return HandleResult(result);
        }

        [HttpGet("my-cart")]
        public async Task<IActionResult> GetMyCart()
        {
            var result = await _cartService.GetMyCartAsync(GetUserId());
            return HandleResult(result);
        }

        [HttpPut("selection")]
        public async Task<IActionResult> UpdateSelection([FromBody] UpdateCartItemSelectionDTO dto)
        {
            var result = await _cartService.UpdateCartItemSelectionAsync(GetUserId(), dto);
            return HandleResult(result);
        }

        [HttpDelete("items/{cartItemId}")]
        public async Task<IActionResult> RemoveCartItem(Guid cartItemId)
        {
            var result = await _cartService.RemoveCartItemAsync(GetUserId(), cartItemId);
            return HandleResult(result);
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CreateOrderFromCartDTO dto)
        {
            var result = await _cartService.CreateOrderFromSelectedCartAsync(GetUserId(), dto);
            return HandleResult(result);
        }


        [HttpPost("preview-checkout")]
        public async Task<IActionResult> PreviewCheckout([FromBody] PreviewCheckoutDTO dto)
        {
            var result = await _cartService.PreviewCheckoutAsync(GetUserId(), dto);
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

            return Ok(result);
        }
    }
}