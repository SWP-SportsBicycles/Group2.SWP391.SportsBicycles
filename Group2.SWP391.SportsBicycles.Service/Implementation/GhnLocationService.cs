using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class GhnLocationService : IGhnLocationService
    {
        private readonly HttpClient _httpClient;
        private readonly GhnSettings _settings;

        // chỉ hỗ trợ 3 tỉnh
        private static readonly HashSet<string> SupportedProvinceNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Hồ Chí Minh",
            "Hà Nội",
            "Đà Nẵng"
        };

        public GhnLocationService(
            HttpClient httpClient,
            IOptions<GhnSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;

            _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Token", _settings.Token);
        }

        private static ResponseDTO Success(object? data = null,
            BusinessCode code = BusinessCode.GET_DATA_SUCCESSFULLY)
            => new()
            {
                IsSucess = true,
                BusinessCode = code,
                Data = data
            };

        private static ResponseDTO Fail(BusinessCode code, string msg)
            => new()
            {
                IsSucess = false,
                BusinessCode = code,
                Message = msg
            };

        public async Task<ResponseDTO> GetSupportedProvincesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("master-data/province");
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return Fail(BusinessCode.INTERNAL_ERROR, $"GHN province error: {body}");

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var code = root.GetProperty("code").GetInt32();
                if (code != 200)
                {
                    var message = root.TryGetProperty("message", out var msg)
                        ? msg.GetString()
                        : body;
                    return Fail(BusinessCode.INVALID_ACTION, message ?? "Không lấy được danh sách tỉnh/thành");
                }

                var data = root.GetProperty("data");

                var provinces = new List<ProvinceDTO>();

                foreach (var item in data.EnumerateArray())
                {
                    var provinceName = item.GetProperty("ProvinceName").GetString() ?? string.Empty;

                    if (!IsSupportedProvince(provinceName))
                        continue;

                    provinces.Add(new ProvinceDTO
                    {
                        ProvinceId = item.GetProperty("ProvinceID").GetInt32(),
                        ProvinceName = provinceName
                    });
                }

                var ordered = provinces
                    .OrderBy(x => x.ProvinceName)
                    .ToList();

                return Success(ordered);
            }
            catch (Exception ex)
            {
                return Fail(BusinessCode.INTERNAL_ERROR, ex.Message);
            }
        }

        public async Task<ResponseDTO> GetDistrictsAsync(int provinceId)
        {
            if (provinceId <= 0)
                return Fail(BusinessCode.INVALID_INPUT, "provinceId không hợp lệ");

            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    province_id = provinceId
                });

                var response = await _httpClient.PostAsync(
                    "master-data/district",
                    new StringContent(payload, Encoding.UTF8, "application/json"));

                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return Fail(BusinessCode.INTERNAL_ERROR, $"GHN district error: {body}");

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var code = root.GetProperty("code").GetInt32();
                if (code != 200)
                {
                    var message = root.TryGetProperty("message", out var msg)
                        ? msg.GetString()
                        : body;
                    return Fail(BusinessCode.INVALID_ACTION, message ?? "Không lấy được danh sách quận/huyện");
                }

                var data = root.GetProperty("data");

                var districts = new List<DistrictDTO>();

                foreach (var item in data.EnumerateArray())
                {
                    districts.Add(new DistrictDTO
                    {
                        DistrictId = item.GetProperty("DistrictID").GetInt32(),
                        DistrictName = item.GetProperty("DistrictName").GetString() ?? string.Empty
                    });
                }

                return Success(districts.OrderBy(x => x.DistrictName).ToList());
            }
            catch (Exception ex)
            {
                return Fail(BusinessCode.INTERNAL_ERROR, ex.Message);
            }
        }

        public async Task<ResponseDTO> GetWardsAsync(int districtId)
        {
            if (districtId <= 0)
                return Fail(BusinessCode.INVALID_INPUT, "districtId không hợp lệ");

            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    district_id = districtId
                });

                var response = await _httpClient.PostAsync(
                    "master-data/ward",
                    new StringContent(payload, Encoding.UTF8, "application/json"));

                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return Fail(BusinessCode.INTERNAL_ERROR, $"GHN ward error: {body}");

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var code = root.GetProperty("code").GetInt32();
                if (code != 200)
                {
                    var message = root.TryGetProperty("message", out var msg)
                        ? msg.GetString()
                        : body;
                    return Fail(BusinessCode.INVALID_ACTION, message ?? "Không lấy được danh sách phường/xã");
                }

                var data = root.GetProperty("data");

                var wards = new List<WardDTO>();

                foreach (var item in data.EnumerateArray())
                {
                    wards.Add(new WardDTO
                    {
                        WardCode = item.GetProperty("WardCode").GetString() ?? string.Empty,
                        WardName = item.GetProperty("WardName").GetString() ?? string.Empty
                    });
                }

                return Success(wards.OrderBy(x => x.WardName).ToList());
            }
            catch (Exception ex)
            {
                return Fail(BusinessCode.INTERNAL_ERROR, ex.Message);
            }
        }

        private static bool IsSupportedProvince(string provinceName)
        {
            var normalized = provinceName.Trim();

            return SupportedProvinceNames.Contains(normalized)
                || normalized.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("Đà Nẵng", StringComparison.OrdinalIgnoreCase);
        }
    }
}
