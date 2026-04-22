using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Group2.SWP391.SportsBicycles.API.Controllers.AdminController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = nameof(RoleEnum.ADMIN))]
    public class AdminOrderController : ControllerBase
    {
        private readonly IAdminOrderService _service;

        public AdminOrderController(IAdminOrderService service)
        {
            _service = service;
        }

        // ================= LIST ORDERS =================
        [HttpGet]
        public async Task<IActionResult> GetOrders(
            int page = 1,
            int size = 10,
            OrderStatusEnum? status = null)
        {
            var result = await _service.GetOrdersAsync(page, size, status);
            return Ok(result);
        }

        // ================= NOTIFY SELLER =================
        [HttpPost("{orderId}/notify-seller")]
        public async Task<IActionResult> NotifySeller(Guid orderId)
        {
            var result = await _service.NotifySellerAsync(orderId);
            return Ok(result);
        }

        // ================= CONFIRM PAYOUT =================
        [HttpPost("{orderId}/confirm-payout")]
        public async Task<IActionResult> ConfirmPayout(Guid orderId)
        {
            var result = await _service.ConfirmPayoutAsync(orderId);
            return Ok(result);
        }
    }
}
