using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.BuyerController
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _service;

        public ReviewController(IReviewService service)
        {
            _service = service;
        }
        private Guid GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User chưa đăng nhập");

            return Guid.Parse(userId);
        }

        [Authorize(Roles = nameof(RoleEnum.BUYER))]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewDTO dto)
        {
            var result = await _service.CreateReviewAsync(GetUserId(), dto);
            return StatusCode(result.IsSucess ? 200 : 400, result);
        }

        [Authorize(Roles = nameof(RoleEnum.SELLER))]
        [HttpGet("my-reviews-for-seller")]
        public async Task<IActionResult> GetMyReviews()
        {
            var result = await _service.GetMyReviewsAsync(GetUserId());
            return StatusCode(result.IsSucess ? 200 : 400, result);
        }

    }
}
