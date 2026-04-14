using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.SellerController
{
    [ApiController]
    [Route("api/seller-listing")]
    public class SellerListingController : ControllerBase
    {
        private readonly ISellerListingService _service;

        public SellerListingController(ISellerListingService service)
        {
            _service = service;
        }

        private Guid GetUserId()
        {
            return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ListingCreateDTO dto)
        {
            var result = await _service.CreateAsync(GetUserId(), dto);
            return HandleResult(result);
        }

        // ================= SUBMIT =================
        [HttpPost("{listingId}/submit")]
        public async Task<IActionResult> Submit(Guid listingId)
        {
            var result = await _service.SubmitForReviewAsync(GetUserId(), listingId);
            return HandleResult(result);
        }

        // ================= UPDATE =================
        [HttpPut("{listingId}")]
        public async Task<IActionResult> Update(Guid listingId, [FromBody] ListingUpsertDTO dto)
        {
            var result = await _service.UpdateAsync(GetUserId(), listingId, dto);
            return HandleResult(result);
        }

        // ================= DELETE =================
        [HttpDelete("{listingId}")]
        public async Task<IActionResult> Delete(Guid listingId)
        {
            var result = await _service.DeleteAsync(GetUserId(), listingId);
            return HandleResult(result);
        }

        // ================= GET MY LIST =================
        [HttpGet]
        public async Task<IActionResult> GetMyListings(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetMyListingsAsync(GetUserId(), pageNumber, pageSize);
            return HandleResult(result);
        }

        // ================= GET DETAIL =================
        [HttpGet("{listingId}")]
        public async Task<IActionResult> GetDetail(Guid listingId)
        {
            var result = await _service.GetDetailsAsync(GetUserId(), listingId);
            return HandleResult(result);
        }

        // ================= WITHDRAW =================
        [HttpPost("{listingId}/withdraw")]
        public async Task<IActionResult> Withdraw(Guid listingId)
        {
            var result = await _service.WithdrawAsync(GetUserId(), listingId);
            return HandleResult(result);
        }

        // ================= VALIDATE =================
        [HttpGet("{listingId}/validate")]
        public async Task<IActionResult> Validate(Guid listingId)
        {
            var result = await _service.ValidateListingAsync(GetUserId(), listingId);
            return HandleResult(result);
        }

        // ================= HANDLE =================
        private IActionResult HandleResult(ResponseDTO result)
        {
            if (result == null)
            {
                return StatusCode(500, new ResponseDTO
                {
                    IsSucess = false,
                    BusinessCode = BusinessCode.INTERNAL_ERROR
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
                    or BusinessCode.VALIDATION_ERROR => BadRequest(result),

                    BusinessCode.ACCESS_DENIED => Forbid(),

                    _ => StatusCode(500, result)
                };
            }

            return Ok(result);
        }
    }
}
