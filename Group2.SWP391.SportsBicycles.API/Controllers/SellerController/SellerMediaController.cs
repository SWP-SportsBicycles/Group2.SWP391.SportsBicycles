using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.SellerController
{
    [Route("api/seller-media")]
    [ApiController]
    [Authorize]
    public class SellerMediaController : ControllerBase
    {
        private readonly ISellerMediaService _service;

        public SellerMediaController(ISellerMediaService service)
        {
            _service = service;
        }

        private Guid GetUserId()
        {
            return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // ================= UPLOAD =================
        [HttpPost("upload-multiple")]
        public async Task<IActionResult> UploadMultiple([FromForm] Guid listingId, [FromForm] List<IFormFile> files)
        {
            var result = await _service.UploadMultipleAsync(GetUserId(), listingId, files);
            return Ok(result);
        }

        // ================= DELETE =================
        [HttpDelete("{mediaId}")]
        public async Task<IActionResult> Delete(Guid mediaId)
        {
            var result = await _service.DeleteAsync(GetUserId(), mediaId);
            return Ok(result);
        }

        // ================= UPDATE TYPE =================
        [HttpPut("{mediaId}/type")]
        public async Task<IActionResult> UpdateType(Guid mediaId, [FromQuery] MediaType type)
        {
            var result = await _service.UpdateTypeAsync(GetUserId(), mediaId, type);
            return Ok(result);
        }
    }
}
