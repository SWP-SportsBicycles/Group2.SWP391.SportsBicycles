using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class PayOSService : IPayOSService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public PayOSService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string> CreatePaymentLink(long orderCode, int amount)
        {
            if (orderCode <= 0)
                throw new ArgumentException("orderCode không hợp lệ", nameof(orderCode));

            if (amount <= 0)
                throw new ArgumentException("amount phải lớn hơn 0", nameof(amount));

            var clientId = GetRequiredConfig("PayOS:ClientId");
            var apiKey = GetRequiredConfig("PayOS:ApiKey");
            var checksumKey = GetRequiredConfig("PayOS:ChecksumKey");
            var returnUrl = GetRequiredConfig("PayOS:ReturnUrl");
            var cancelUrl = GetRequiredConfig("PayOS:CancelUrl");

            var description = $"Bike-{orderCode}";
            var signature = CreateSignature(orderCode, amount, description, returnUrl, cancelUrl, checksumKey);

            var body = new
            {
                orderCode,
                amount,
                description,
                returnUrl,
                cancelUrl,
                items = new[]
                {
                    new
                    {
                        name = "Bike Order",
                        quantity = 1,
                        price = amount
                    }
                },
                signature
            };

            var jsonBody = JsonConvert.SerializeObject(body);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api-merchant.payos.vn/v2/payment-requests"
            );

            request.Headers.Add("x-client-id", clientId);
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("Accept", "application/json");
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"PayOS create payment link failed. HTTP {(int)response.StatusCode} - {response.ReasonPhrase}. Response: {content}"
                );
            }

            JObject json;
            try
            {
                json = JObject.Parse(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"PayOS response không phải JSON hợp lệ. Response: {content}", ex);
            }

            var checkoutUrl = json["data"]?["checkoutUrl"]?.ToString();

            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                var code = json["code"]?.ToString();
                var desc = json["desc"]?.ToString();

                throw new Exception(
                    $"PayOS không trả về checkoutUrl. Code: {code}, Desc: {desc}, Response: {content}"
                );
            }

            return checkoutUrl;
        }

        private string CreateSignature(
            long orderCode,
            int amount,
            string description,
            string returnUrl,
            string cancelUrl,
            string checksumKey)
        {
            var rawData =
                $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private string GetRequiredConfig(string key)
        {
            var value = _config[key];

            if (string.IsNullOrWhiteSpace(value))
                throw new Exception($"Thiếu cấu hình bắt buộc: {key}");

            return value;
        }
    }
}