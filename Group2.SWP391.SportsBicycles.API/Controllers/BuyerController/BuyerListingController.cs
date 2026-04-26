using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Group2.SWP391.SportsBicycles.API.Controllers.BuyerController
{
    [ApiController]
    [Route("api/buyer-listing")]
    [AllowAnonymous]

    public class BuyerListingController : BaseController
    {
        private readonly IBuyerListingService _service;

        public BuyerListingController(IBuyerListingService service)
        {
            _service = service;
        }

        // ================= LIST =================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetAllAsync(pageNumber, pageSize);
            return HandleResult(result);
        }

        // ================= DETAIL =================
        [HttpGet("{listingId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetail(Guid listingId)
        {
            var result = await _service.GetDetail(listingId);
            return HandleResult(result);
        }

        // ================= SEARCH =================
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(
        [FromQuery] string? keyword,
        [FromQuery] string? brand,
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? frameSize,
        [FromQuery] string? condition,
        [FromQuery] string? city,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var result = await _service.SearchAsync(
                keyword?.Trim(),
                brand?.Trim(),
                category?.Trim(),
                minPrice,
                maxPrice,
                frameSize?.Trim(),
                condition?.Trim(),
                city?.Trim(),
                pageNumber,
                pageSize
            );

            return HandleResult(result);
        }

       
    }
}
