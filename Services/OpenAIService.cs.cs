
using OpenAI;
using OpenAI.Chat;
using System.Text;
using TesteLLMs.Interfaces;
using TesteLLMs.Models;  // dependendo do namespace do SDK

public class OpenAIService : IAIService
{
    private readonly ChatClient _chatClient;

    public OpenAIService(string apiKey)
    {
        // Cliente direto para OpenAI (não Azure)
        var client = new OpenAIClient(apiKey);

        // Escolha o modelo (ex: gpt-4o-mini, gpt-4.1, gpt-3.5-turbo)
        _chatClient = client.GetChatClient("gpt-4o-mini");

    }

    public bool ValidateApiKey(string apiKey)
    {
        // Aqui você pode validar o formato da chave OpenAI (começa com "sk-")
        return !string.IsNullOrEmpty(apiKey) && apiKey.StartsWith("sk-");
    }

    private async Task<string> SendChatAsync(List<ChatMessage> messages)
    {
        var response = await _chatClient.CompleteChatAsync(messages);

        // Normalmente o conteúdo vem no último item de Content
        return response.Value.Content[0].Text;
    }

    public async Task<string> GenerateResponse(string prompt)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("Você é um assistente especializado em turismo. Use APENAS as informações fornecidas no contexto."),
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
}
