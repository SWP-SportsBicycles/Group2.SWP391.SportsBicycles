using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IAdminOrderService
    {
        Task<ResponseDTO> GetOrdersAsync(int page, int size, OrderStatusEnum? status);
        Task<ResponseDTO> NotifySellerAsync(Guid orderId);
        Task<ResponseDTO> ConfirmPayoutAsync(Guid orderId);
    }
}
