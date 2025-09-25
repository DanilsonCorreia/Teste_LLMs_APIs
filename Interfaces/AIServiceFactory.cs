using TesteLLMs.Interfaces;
using TesteLLMs.Services;

public static class AIServiceFactory
{
    public static IAIService CreateService(string apiKey, string datasetPath)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("API key não pode ser nula ou vazia.");

        // OpenAI (chaves oficiais começam com "sk-")
        if (apiKey.StartsWith("sk-"))
            return new OpenAIService(apiKey);

        //// Azure OpenAI (exemplo: chave com 32 caracteres hex)
        //if (apiKey.Length == 32 && apiKey.All(char.IsLetterOrDigit))
        //    return new AzureOpenAIService(apiKey, datasetPath); // você cria essa classe

        // Google Gemini (simulação: chave começa com "AI")
        if (apiKey.StartsWith("AI"))
            return new GeminiService(apiKey);

        //// Anthropic Claude (simulação: chave começa com "claude-")
        //if (apiKey.StartsWith("claude-"))
        //    return new AnthropicClaudeService(apiKey, datasetPath);

        //// HuggingFace (simulação: chave começa com "hf_")
        //if (apiKey.StartsWith("hf_"))
        //    return new HuggingFaceService(apiKey, datasetPath);

        throw new NotSupportedException("Tipo de chave não reconhecido.");
    }
}
