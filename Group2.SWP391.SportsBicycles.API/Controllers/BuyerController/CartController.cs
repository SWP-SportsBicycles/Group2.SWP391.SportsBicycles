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
    public class CartController : BaseController
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

    }
}