using Group2.SWP391.SportsBicycles.Services.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class FakeShippingProviderClient : IShippingProviderClient
    {
        public async Task<(bool IsSuccess, string? ProviderOrderCode, string? ErrorMessage)> CreateOrderAsync(
            string provider,
            string senderName,
            string senderPhone,
            string senderAddress,
            string receiverName,
            string receiverPhone,
            string receiverAddress,
            decimal codAmount,
            string? note)
        {
            await Task.Delay(300);

            return (true, $"FAKE-{Guid.NewGuid().ToString("N")[..10].ToUpper()}", null);
        }

        public async Task<(bool IsSuccess, string? RawStatus, string? Description, string? Location, DateTime? EventTime, string? ErrorMessage)> TrackOrderAsync(
            string provider,
            string providerOrderCode)
        {
            await Task.Delay(300);

            return (
                true,
                "in_transit",
                "Đơn hàng đang được vận chuyển",
                "HCM Hub",
                DateTime.UtcNow,
                null
            );
        }
    }
}
