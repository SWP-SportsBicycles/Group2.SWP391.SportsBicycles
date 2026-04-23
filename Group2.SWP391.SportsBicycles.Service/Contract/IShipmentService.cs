using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IShipmentService
    {
        Task<ResponseDTO> CreateShipmentAsync(Guid orderId, CreateShipmentDTO dto);
        Task<ResponseDTO> GetShipmentByOrderIdAsync(Guid orderId);
        Task<ResponseDTO> SyncTrackingAsync(Guid orderId);
        Task<ResponseDTO> ConfirmReceivedAsync(Guid buyerId, Guid orderId);



    }
}
