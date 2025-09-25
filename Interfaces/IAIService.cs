using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TesteLLMs.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateResponse(string prompt);
        bool ValidateApiKey(string apiKey);
    }

    public enum AIServiceType
    {
        OpenAI,
        AzureOpenAI,
        GoogleGemini,
        AnthropicClaude,
        HuggingFace
    }
}
