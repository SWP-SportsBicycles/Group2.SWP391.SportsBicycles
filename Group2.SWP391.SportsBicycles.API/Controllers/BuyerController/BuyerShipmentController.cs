using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Controllers.BuyerController
{
    [ApiController]
    [Route("api/buyer-shipment")]
    [Authorize(Roles = "BUYER")]

    public class BuyerShipmentController : BaseController
    {
        private readonly IShipmentService _service;

        public BuyerShipmentController(IShipmentService service)
        {
            _service = service;
        }

        // ================= CREATE SHIPMENT =================
       

        // ================= GET SHIPMENT BY ORDER =================
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetShipmentByOrderId(Guid orderId)
        {
            var result = await _service.GetShipmentByOrderIdAsync(orderId);
            return HandleResult(result);
        }

        // ================= SYNC TRACKING =================
        [HttpPost("sync/{orderId}")]
        public async Task<IActionResult> SyncTracking(Guid orderId)
        {
            var result = await _service.SyncTrackingAsync(orderId);
            return HandleResult(result);
        }


        [HttpPost("confirm-received/{orderId}")]
        public async Task<IActionResult> ConfirmReceived(Guid orderId)
        {
            var buyerIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("userId")?.Value;

            if (string.IsNullOrWhiteSpace(buyerIdClaim))
                return Unauthorized("Không lấy được buyerId từ token");

            var buyerId = Guid.Parse(buyerIdClaim);

            var result = await _service.ConfirmReceivedAsync(buyerId, orderId);
            return HandleResult(result);
        }


    
    }
}
