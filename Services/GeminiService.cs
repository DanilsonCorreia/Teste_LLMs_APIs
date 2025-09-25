using Mscc.GenerativeAI;
using OpenAI.Assistants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TesteLLMs.Interfaces;

namespace TesteLLMs.Services
{
    public class GeminiService : IAIService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        private readonly string _systemInstruction = "Você é um assistente especializado em turismo. Use APENAS as informações fornecidas no contexto.";

        public GeminiService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/"); // Endereço base da API Gemini
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }


        public async Task<string> GenerateResponse(string prompt)
        {

            var contents = new List<object>
            {
                // 1. Mensagem do Sistema (instruções de comportamento)
                new
                {
                    role = "user", // Para o Gemini, instruções do sistema geralmente vão como "user" na primeira mensagem
                    parts = new[] { new { text = "Você é um assistente especializado em turismo. Use APENAS as informações fornecidas no contexto." } }
                },
                // 2. Mensagem do Usuário (o prompt real)
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            };
            var requestBody = new
            {
                contents,
                generationConfig = new
                {
                    temperature = 0.7, // Controla a criatividade da resposta (0.0 a 1.0)
                    //maxOutputTokens = 200 // Limite de tokens na resposta
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // O endpoint pode variar, verifique a documentação oficial do Gemini
            var response = await _httpClient.PostAsync($"models/gemini-2.5-flash:generateContent?key={_apiKey}", content);

            response.EnsureSuccessStatusCode(); // Lança exceção se o código de status HTTP indicar erro

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(responseBody);

            // Parsear a resposta para extrair o texto gerado
            // A estrutura da resposta pode variar, adapte conforme a documentação da API
            if (jsonDocument.RootElement.TryGetProperty("candidates", out var candidates))
            {
                if (candidates.GetArrayLength() > 0)
                {
                    if (candidates[0].TryGetProperty("content", out var contentElement) &&
                        contentElement.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        if (parts[0].TryGetProperty("text", out var textElement))
                        {
                            return textElement.GetString();
                        }
                    }
                }
            }
            return "Nenhuma resposta de texto encontrada.";
        }

        // Você pode também ter um método mais flexível que aceita uma lista de mensagens
        public async Task<string> GenerateChatResponse(List<(string role, string text)> chatHistory)
        {
            var contents = new List<object>();

            // Adicione a instrução do sistema como a primeira mensagem
            contents.Add(new
            {
                role = "user", // Gemini interpreta instruções iniciais como "user"
                parts = new[] { new { text = _systemInstruction } }
            });

            // Adicione o histórico da conversa
            foreach (var message in chatHistory)
            {
                contents.Add(new
                {
                    role = message.role, // 'user' ou 'model'
                    parts = new[] { new { text = message.text } }
                });
            }

            var requestBody = new
            {
                contents = contents,
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1000
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"models/gemini-2.5-flash:generateContent?key={_apiKey}", content);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(responseBody);

            if (jsonDocument.RootElement.TryGetProperty("candidates", out var candidates))
            {
                if (candidates.GetArrayLength() > 0)
                {
                    if (candidates[0].TryGetProperty("content", out var contentElement) &&
                        contentElement.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        if (parts[0].TryGetProperty("text", out var textElement))
                        {
                            return textElement.GetString();
                        }
                    }
                }
            }
            return "Nenhuma resposta de texto encontrada.";
        }



        public bool ValidateApiKey(string apiKey)
        {
            throw new NotImplementedException();
        }
    }
}
