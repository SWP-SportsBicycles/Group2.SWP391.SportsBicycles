using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class GhnShippingProviderClient : IShippingProviderClient
    {
        private readonly HttpClient _httpClient;
        private readonly GhnSettings _settings;

        public GhnShippingProviderClient(
            HttpClient httpClient,
            IOptions<GhnSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));

            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
                throw new ArgumentException("GhnSettings:BaseUrl chưa được cấu hình");

            if (string.IsNullOrWhiteSpace(_settings.Token))
                throw new ArgumentException("GhnSettings:Token chưa được cấu hình");

            if (string.IsNullOrWhiteSpace(_settings.ShopId))
                throw new ArgumentException("GhnSettings:ShopId chưa được cấu hình");

            _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Clear();

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            _httpClient.DefaultRequestHeaders.Add("Token", _settings.Token);
            _httpClient.DefaultRequestHeaders.Add("ShopId", _settings.ShopId);
        }

        public async Task<(bool IsSuccess, string? ProviderOrderCode, string? TrackingUrl, string? ErrorMessage)> CreateOrderAsync(
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
            string? note)
        {
            try
            {
                if (!string.Equals(provider, "GHN", StringComparison.OrdinalIgnoreCase))
                    return (false, null, null, "Provider không được hỗ trợ");

                if (fromDistrictId <= 0)
                    return (false, null, null, "fromDistrictId không hợp lệ");

                if (toDistrictId <= 0)
                    return (false, null, null, "toDistrictId không hợp lệ");

                if (string.IsNullOrWhiteSpace(fromWardCode))
                    return (false, null, null, "fromWardCode không được để trống");

                if (string.IsNullOrWhiteSpace(toWardCode))
                    return (false, null, null, "toWardCode không được để trống");

                var serviceResult = await GetAvailableServiceAsync(fromDistrictId, toDistrictId);
                if (!serviceResult.IsSuccess || serviceResult.ServiceId == null)
                    return (false, null, null, serviceResult.ErrorMessage ?? "Không lấy được service GHN");

                var payload = new
                {
                    payment_type_id = 1,
                    note = string.IsNullOrWhiteSpace(note) ? "Tạo đơn từ hệ thống" : note,
                    required_note = "KHONGCHOXEMHANG",

                    from_name = senderName,
                    from_phone = senderPhone,
                    from_address = senderAddress,
                    from_district_id = fromDistrictId,
                    from_ward_code = fromWardCode,

                    to_name = receiverName,
                    to_phone = receiverPhone,
                    to_address = receiverAddress,
                    to_district_id = toDistrictId,
                    to_ward_code = toWardCode,

                    service_id = serviceResult.ServiceId.Value,

                    weight = 1000,
                    length = 30,
                    width = 20,
                    height = 10,

                    cod_amount = codAmount,
                    insurance_value = 0,
                    content = "Bike shipment",

                    items = new[]
                    {
                        new
                        {
                            name = "Sports Bicycle",
                            quantity = 1,
                            price = 0,
                            length = 30,
                            width = 20,
                            height = 10,
                            weight = 1000,
                            category = new
                            {
                                level1 = "Xe đạp"
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                Console.WriteLine("GHN CREATE PAYLOAD: " + json);

                var response = await _httpClient.PostAsync(
                    "v2/shipping-order/create",
                    new StringContent(json, Encoding.UTF8, "application/json"));

                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("GHN CREATE RESPONSE: " + body);

                if (!response.IsSuccessStatusCode)
                    return (false, null, null, body);

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var code = root.GetProperty("code").GetInt32();
                if (code != 200)
                {
                    var message = root.TryGetProperty("message", out var msg)
                        ? msg.GetString()
                        : body;
                    return (false, null, null, message);
                }

                var data = root.GetProperty("data");
                var orderCode = data.GetProperty("order_code").GetString();

                // GHN không trả tracking url public trực tiếp ở API create
                // tạm build link tra cứu theo mã đơn
                var trackingUrl = !string.IsNullOrWhiteSpace(orderCode)
                    ? $"https://donhang.ghn.vn/?order_code={orderCode}"
                    : null;

                return (true, orderCode, trackingUrl, null);
            }
            catch (Exception ex)
            {
                return (false, null, null, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, string? RawStatus, string? Description, string? Location, DateTime? EventTime, string? ErrorMessage)> TrackOrderAsync(
            string provider,
            string providerOrderCode)
        {
            try
            {
                if (!string.Equals(provider, "GHN", StringComparison.OrdinalIgnoreCase))
                    return (false, null, null, null, null, "Provider không được hỗ trợ");

                var payload = new
                {
                    order_code = providerOrderCode
                };

                var json = JsonSerializer.Serialize(payload);
                var response = await _httpClient.PostAsync(
                    "v2/shipping-order/detail",
                    new StringContent(json, Encoding.UTF8, "application/json"));

                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("GHN TRACK RESPONSE: " + body);

                if (!response.IsSuccessStatusCode)
                    return (false, null, null, null, null, body);

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var code = root.GetProperty("code").GetInt32();
                if (code != 200)
                {
                    var message = root.TryGetProperty("message", out var msg)
                        ? msg.GetString()
                        : body;
                    return (false, null, null, null, null, message);
                }

                var data = root.GetProperty("data");
                var status = data.TryGetProperty("status", out var s) ? s.GetString() : "pending";

                DateTime? eventTime = null;
                if (data.TryGetProperty("updated_date", out var updated) &&
                    updated.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(updated.GetString(), out var parsedDate))
                {
                    eventTime = parsedDate;
                }

                string? location = null;
                if (data.TryGetProperty("current_warehouse", out var warehouse) &&
                    warehouse.ValueKind == JsonValueKind.String)
                {
                    location = warehouse.GetString();
                }

                var description = !string.IsNullOrWhiteSpace(status)
                    ? $"GHN: {status}"
                    : "GHN tracking update";

                return (true, status, description, location, eventTime ?? DateTime.UtcNow, null);
            }
            catch (Exception ex)
            {
                return (false, null, null, null, null, ex.Message);
            }
        }

        private async Task<(bool IsSuccess, int? ServiceId, string? ErrorMessage)> GetAvailableServiceAsync(
            int fromDistrictId,
            int toDistrictId)
        {
            try
            {
                var payload = new
                {
                    shop_id = int.Parse(_settings.ShopId),
                    from_district = fromDistrictId,
                    to_district = toDistrictId
                };

                var json = JsonSerializer.Serialize(payload);
                Console.WriteLine("GHN SERVICE PAYLOAD: " + json);

                var response = await _httpClient.PostAsync(
                    "v2/shipping-order/available-services",
                    new StringContent(json, Encoding.UTF8, "application/json"));

                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("GHN SERVICE RESPONSE: " + body);

                if (!response.IsSuccessStatusCode)
                    return (false, null, body);

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var code = root.GetProperty("code").GetInt32();
                if (code != 200)
                {
                    var message = root.TryGetProperty("message", out var msg)
                        ? msg.GetString()
                        : body;
                    return (false, null, message);
                }

                var data = root.GetProperty("data");
                if (data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0)
                    return (false, null, "GHN không trả về service khả dụng");

                var firstService = data[0];
                var serviceId = firstService.GetProperty("service_id").GetInt32();

                return (true, serviceId, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }
    }
}