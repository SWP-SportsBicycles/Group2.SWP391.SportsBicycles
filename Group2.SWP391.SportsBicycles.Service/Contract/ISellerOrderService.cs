using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface ISellerOrderService
    {
        Task<ResponseDTO> GetMyOrdersAsync(Guid sellerId, int page, int size);
        Task<ResponseDTO> GetOrderDetailAsync(Guid sellerId, Guid orderId);

        Task<ResponseDTO> ConfirmOrderAsync(Guid sellerId, Guid orderId);
        Task<ResponseDTO> CancelOrderAsync(Guid sellerId, Guid orderId);
    }
}
