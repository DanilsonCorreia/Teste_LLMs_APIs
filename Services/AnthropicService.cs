using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TesteLLMs.Services
{
    public class AnthropicService
    {
        private readonly HttpClient _httpClient;

        public AnthropicService(string apiKey)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        public async Task<string> AskAsync(string prompt)
        {
            var payload = new
            {
                model = "claude-3-5-sonnet-20240620",
                max_tokens = 500,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
