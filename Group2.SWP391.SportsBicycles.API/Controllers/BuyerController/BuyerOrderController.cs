using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Group2.SWP391.SportsBicycles.API.Controllers.BuyerController
{
    [ApiController]
    [Route("api/buyer-order")]
    [Authorize(Roles = "BUYER")]
    public class BuyerOrderController : ControllerBase
    {
        private readonly IBuyerOrderService _service;

        public BuyerOrderController(IBuyerOrderService service)
        {
            _service = service;
        }

        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;

            var userIdClaim = User.Claims.FirstOrDefault(c =>
                c.Type == JwtRegisteredClaimNames.Sub ||
                c.Type == ClaimTypes.NameIdentifier ||
                c.Type.EndsWith("/nameidentifier"));

            if (userIdClaim == null)
                return false;

            return Guid.TryParse(userIdClaim.Value, out userId);
        }

        private IActionResult UnauthorizedUser()
        {
            return Unauthorized(new ResponseDTO
            {
                IsSucess = false,
                BusinessCode = BusinessCode.AUTH_NOT_FOUND,
                Message = "Không xác định được người dùng"
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDTO dto)
        {
            if (!TryGetUserId(out var buyerId))
                return UnauthorizedUser();

            var result = await _service.CreateOrderAsync(buyerId, dto);
            return HandleResult(result);
        }

        [HttpPost("{orderId}/paid")]
        public async Task<IActionResult> MarkPaid([FromRoute] Guid orderId)
        {
            var result = await _service.MarkOrderPaidAsync(orderId);
            return HandleResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!TryGetUserId(out var buyerId))
                return UnauthorizedUser();

            var result = await _service.GetMyOrdersAsync(buyerId, pageNumber, pageSize);
            return HandleResult(result);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderDetail(Guid orderId)
        {
            if (!TryGetUserId(out var buyerId))
                return UnauthorizedUser();

            var result = await _service.GetOrderDetailAsync(buyerId, orderId);
            return HandleResult(result);
        }

        private IActionResult HandleResult(ResponseDTO result)
        {
            if (result == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDTO
                {
                    IsSucess = false,
                    BusinessCode = BusinessCode.INTERNAL_ERROR,
                    Message = "Lỗi hệ thống"
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
                    or BusinessCode.VALIDATION_ERROR
                    or BusinessCode.VALIDATION_FAILED => BadRequest(result),

                    BusinessCode.AUTH_NOT_FOUND
                    or BusinessCode.WRONG_PASSWORD => Unauthorized(result),

                    BusinessCode.ACCESS_DENIED
                    or BusinessCode.PERMISSION_DENIED => StatusCode(StatusCodes.Status403Forbidden, result),

                    BusinessCode.EXCEPTION
                    or BusinessCode.INTERNAL_ERROR => StatusCode(StatusCodes.Status500InternalServerError, result),

                    BusinessCode.CREATED_SUCCESSFULLY => StatusCode(StatusCodes.Status201Created, result),

                    _ => Ok(result)
                };
            }

            return Ok(result);
        }
    }
}