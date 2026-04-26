using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Mvc;

namespace Group2.SWP391.SportsBicycles.API.Controllers
{
    [ApiController]
    [Route("api/fake-shipment")]
    public class FakeShipmentController : ControllerBase
    {
        private readonly IFakeShipmentService _fakeService;

        public FakeShipmentController(IFakeShipmentService fakeService)
        {
            _fakeService = fakeService;
        }

        // ✅ tạo shipment FAKE (không gọi GHN)
        [HttpPost("create/{orderId}")]
        public async Task<IActionResult> Create(Guid orderId)
        {
            var res = await _fakeService.CreateFakeAsync(orderId);
            return Ok(res);
        }

        // ✅ fake delivered
        [HttpPost("delivered/{orderId}")]
        public async Task<IActionResult> Delivered(Guid orderId)
        {
            var res = await _fakeService.DeliveredFakeAsync(orderId);
            return Ok(res);
        }
    }
}