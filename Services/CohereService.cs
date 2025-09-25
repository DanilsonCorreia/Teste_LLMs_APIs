using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TesteLLMs.Services
{
    public class CohereService
    {
        private readonly HttpClient _httpClient;

        public CohereService(string apiKey)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public async Task<string> AskAsync(string prompt)
        {
            var payload = new
            {
                model = "command-r-plus",
                prompt = prompt,
                max_tokens = 300
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.cohere.ai/v1/generate", content);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
