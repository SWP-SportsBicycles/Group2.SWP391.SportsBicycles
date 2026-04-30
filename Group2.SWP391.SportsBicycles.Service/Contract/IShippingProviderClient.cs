using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IShippingProviderClient
    {
        Task<(bool IsSuccess, string? ProviderOrderCode, string? TrackingUrl, string? ErrorMessage)> CreateOrderAsync(
       string provider,
       string senderName,
       string senderPhone,
       string senderAddress,
       int fromDistrictId,
       string? fromWardCode,
       string receiverName,
       string receiverPhone,
       string receiverAddress,
       int toDistrictId,
       string toWardCode,
       int codAmount,
       string? note,
       int weight // 👈 FIX
   );

        Task<(bool IsSuccess, string? RawStatus, string? Description, string? Location, DateTime? EventTime, string? ErrorMessage)> TrackOrderAsync(
            string provider,
            string providerOrderCode);


        Task<(bool IsSuccess, decimal Fee, string? ErrorMessage)> CalculateFeeAsync(
      string provider,
      int fromDistrictId,
      string fromWardCode,
      int toDistrictId,
      string toWardCode,
      int insuranceValue,
      int weight); // 👈 thêm
    }
}
