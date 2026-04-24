using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IShippingFeeService
    {
        Task<decimal> CalculateFeeAsync(
            int fromDistrictId,
            string fromWardCode,
            int toDistrictId,
            string toWardCode,
            decimal orderValue);
    }
}
