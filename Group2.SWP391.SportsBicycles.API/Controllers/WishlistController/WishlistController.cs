using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Group2.SWP391.SportsBicycles.API.Controllers.BuyerController
{
    [ApiController]
    [Route("api/wishlist")]
    [Authorize]
    public class WishlistController : BaseController
    {
        private readonly IWishlistService _service;

        public WishlistController(IWishlistService service)
        {
            _service = service;
        }

        [HttpPost("{bikeId}")]
        public async Task<IActionResult> AddToWishlist([FromRoute] Guid bikeId)
        {
            var result = await _service.AddToWishlistAsync(bikeId);
            return HandleResult(result);
        }

        [HttpDelete("{bikeId}")]
        public async Task<IActionResult> RemoveFromWishlist([FromRoute] Guid bikeId)
        {
            var result = await _service.RemoveFromWishlistAsync(bikeId);
            return HandleResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyWishlist(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetMyWishlistAsync(pageNumber, pageSize);
            return HandleResult(result);
        }

     
    }
}