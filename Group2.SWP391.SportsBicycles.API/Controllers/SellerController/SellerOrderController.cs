using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.SellerController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = nameof(RoleEnum.SELLER))]
    public class SellerOrderController : ControllerBase
    {
        private readonly ISellerOrderService _service;

        public SellerOrderController(ISellerOrderService service)
        {
            _service = service;
        }

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ================= LIST =================
        [HttpGet]
        public async Task<IActionResult> Get(int page = 1, int size = 10)
        {
            var result = await _service.GetMyOrdersAsync(GetUserId(), page, size);
            return Ok(result);
        }

        // ================= DETAIL =================
        [HttpGet("{orderId}")]
        public async Task<IActionResult> Detail(Guid orderId)
        {
            var result = await _service.GetOrderDetailAsync(GetUserId(), orderId);
            return Ok(result);
        }

        // ================= CONFIRM =================
        [HttpPost("{orderId}/confirm")]
        public async Task<IActionResult> Confirm(Guid orderId)
        {
            var result = await _service.ConfirmOrderAsync(GetUserId(), orderId);
            return Ok(result);
        }

        // ================= CANCEL =================
        [HttpPost("{orderId}/cancel")]
        public async Task<IActionResult> Cancel(Guid orderId)
        {
            var result = await _service.CancelOrderAsync(GetUserId(), orderId);
            return Ok(result);
        }
    }
}
