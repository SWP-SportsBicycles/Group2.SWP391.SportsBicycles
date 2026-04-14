using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public ChatService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<ChatResponseDto> AskAsync(ChatRequestDto request)
        {
            var apiKey = _config["Groq:ApiKey"];

            var url = "https://api.groq.com/openai/v1/chat/completions";

            var body = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[]
    {
        new
        {
            role = "system",
            content = @"Bạn là chatbot của hệ thống bán xe đạp thể thao cũ.

Nhiệm vụ:
- Tư vấn mua xe đạp cho khách
- Gợi ý loại xe phù hợp theo nhu cầu và ngân sách
- Trả lời NGẮN GỌN, THỰC TẾ

Quy tắc:
- KHÔNG được tự tạo tên người
- KHÔNG đóng vai cá nhân
- KHÔNG bịa thông tin
- Trả lời như hệ thống bán hàng

Ngữ cảnh:
- Website bán xe đạp thể thao cũ
- Các loại xe: road bike, mountain bike, city bike
- Giá từ 2 triệu đến 20 triệu"
        },
        new { role = "user", content = request.Message }
    }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(httpRequest);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine("GROQ RESPONSE: " + json); // debug

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // ❌ check error
            if (root.TryGetProperty("error", out var error))
            {
                var msg = error.GetProperty("message").GetString();
                throw new Exception("Groq error: " + msg);
            }

            // ✅ parse chuẩn OpenAI
            var reply = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return new ChatResponseDto
            {
                Reply = reply ?? ""
            };
        }
    }
}

