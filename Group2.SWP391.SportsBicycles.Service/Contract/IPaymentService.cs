using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IPaymentService
    {
        Task<ResponseDTO> CreatePaymentLink(Guid buyerId, Guid orderId);

        Task<ResponseDTO> HandlePaymentSuccessAsync(string providerOrderCode);
    }
}
