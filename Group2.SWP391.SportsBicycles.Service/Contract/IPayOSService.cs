using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IPayOSService
    {
        Task<string> CreatePaymentLink(long orderCode, int amount);
    }
}
