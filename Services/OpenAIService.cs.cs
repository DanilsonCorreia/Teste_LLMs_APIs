using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Qdrant.Client.Grpc;
using System.Text.Json;
using TesteLLMs.Interfaces;
using TesteLLMs.Models;


namespace TesteLLMs.Services
{
    public class OpenAIService : IAIService
    {
        private readonly ChatClient _chatClient;
        private readonly EmbeddingClient _embeddingClient;
        private readonly OpenAIClient _client;

        public OpenAIService(string apiKey)
        {
            _client = new OpenAIClient(apiKey);
            _chatClient = _client.GetChatClient("gpt-4o-mini");
            _embeddingClient = _client.GetEmbeddingClient("text-embedding-3-small");
        }

        public bool ValidateApiKey(string apiKey)
        {
            return !string.IsNullOrEmpty(apiKey) && apiKey.StartsWith("sk-");
        }

        private async Task<string> SendChatAsync(List<ChatMessage> messages)
        {
            var response = await _chatClient.CompleteChatAsync(messages);
            return response.Value.Content[0].Text;
        }

        public async Task<string> GenerateResponse(string prompt)
        {
            var messages = new List<ChatMessage>
        {
            new SystemChatMessage("Es um assistente virtual de turismos que so responde a perguntas dento do contexto passado"),
            new UserChatMessage(prompt)
        };

            try
            {
                return await SendChatAsync(messages);
            }
            catch (Exception ex)
            {
                return $"Erro ao consultar OpenAI: {ex.Message}";
            }
        }

        public async Task CriarRoteiroStreaming(string prompt, string contexto, Action<string> onChunkReceived)
        {
            var messages = new List<ChatMessage>
    {
                new SystemChatMessage("Você é um assistente virtual de turismo. Responda APENAS com base no contexto fornecido."),
                new UserChatMessage($@"
                    CONTEXTO - DADOS TURÍSTICOS:
                    {contexto}

                    PERGUNTA DO USUÁRIO:
                    {prompt}

                    INSTRUÇÕES:
                    - Seja útil e informativo
                    - Mantenha a resposta natural e envolvente
                    - Se não houver informação específica, indique isso claramente
                ")
            };

            // StreamChatAsync envia os tokens à medida que são gerados
            await foreach (var chunk in _chatClient.CompleteChatStreamingAsync(messages))
            {
                if (chunk.ContentUpdate.Count > 0)
                {
                    var textoParcial = chunk.ContentUpdate[0].Text ?? "";
                    onChunkReceived(textoParcial);
                }
            }
        }

        public async Task<DadosIntencao> GetIntentFromPrompt(string prompt)
        {
            var query = $"""
                "Retira informações sobre a intenção do user e retorna um objecto:" +
                "\"intencao\": \"\"" +
                "\"localizacao\": \"\"" +
                "A intenção pode ser \"roteiro\", \"poi\", \"rotas\", \"eventos\" ou \"chat\"."

                PERGUNTA DO USUÁRIO: {prompt}
                """;
            var responseText = await GenerateResponse(query);
            try
            {
                responseText = responseText.Trim('\"')
                                     .Replace("\\n", "")
                                     .Replace("\\\"", "\"");

                // ✅ Agora faz o parse corretamente
                var dados = JsonSerializer.Deserialize<DadosIntencao>(
                    responseText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                return dados ?? new DadosIntencao { Intencao = "desconhecida", Localizacao = "" };
            }
            catch (JsonException)
            {
                return new DadosIntencao { Intencao = "desconhecida", Localizacao = "" };
            }
        }

        public async Task<string> CriarRoteiro(string prompt, string contexto)
        {
            var query = $"""
                CONTEXTO - DADOS TURÍSTICOS:
                {contexto}

                PERGUNTA DO USUÁRIO: {prompt}
                INSTRUÇÕES:
                - Baseie sua resposta APENAS nos dados fornecidos acima
                - Seja útil e informativo
                - Mantenha a resposta natural e envolvente
                - Se não tiver informação específica, indique isso claramente
                - Inclua detalhes práticos como melhor época, custos, segurança
                """;
            var reponse = await GenerateResponse(query);
            return reponse;

        }

        // ✅ Criação de um único embedding
        public async Task<float[]> CreateEmbeddingAsync(string text)
        {
            try
            {
                OpenAIEmbedding embedding = await _embeddingClient.GenerateEmbeddingAsync(text);
                return embedding.ToFloats().ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gerar embedding: {ex.Message}");
                return Array.Empty<float>();
            }
        }

        // ✅ Criação de múltiplos embeddings
        public async Task<List<float[]>> CreateEmbeddingsBatchAsync(IEnumerable<string> texts)
        {
            try
            {
                var response = await _embeddingClient.GenerateEmbeddingsAsync(texts.ToArray());
                return response.Value.Select(e => e.ToFloats().ToArray()).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gerar embeddings em lote: {ex.Message}");
                return new List<float[]>();
            }
        }

        
    }
}
