using TesteLLMs.Services;
//using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration; // Add this using directive
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using TesteLLMs; // Add this using directive
using TesteLLMs.Services; // Add this using directive

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json")
    .Build();

// Carregar serviços
var destinationsFilePath = config["DestinationsFilePath"];
var openAI = config["OpenAIKey"];
var gemini = config["GeminiKey"];
var anthropic = new AnthropicService(config["AnthropicKey"]);
var cohere = new CohereService(config["CohereKey"]);

var assistant = new SmartTourismAssistant(gemini, destinationsFilePath);

Console.WriteLine("🌍 ASSISTENTE DE TURISMO INTELIGENTE v2.5.0");
Console.WriteLine("===========================================");

while (true)
{
    Console.WriteLine("\nEscolha uma opção:");
    Console.WriteLine("1. 📍 Consulta sobre destino");
    Console.WriteLine("2. 📋 Gerar roteiro de viagem");
    Console.WriteLine("3. ⚖️ Comparar dois destinos");
    Console.WriteLine("4. 🚪 Sair");
    Console.Write("\nOpção: ");

    var option = Console.ReadLine();

    switch (option)
    {
        case "1":
            await HandleDestinationQuery(assistant);
            break;

        case "2":
            await HandleItineraryRequest(assistant);
            break;

        case "3":
            await HandleComparisonRequest(assistant);
            break;

        case "4":
            Console.WriteLine("Obrigado por usar o assistente! 🌴");
            return;

        default:
            Console.WriteLine("Opção inválida. Tente novamente.");
            break;
    }
}

static async Task HandleDestinationQuery(SmartTourismAssistant assistant)
{
    Console.Write("\n📍 Sobre qual destino você quer informações? ");
    var query = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(query)) return;

    Console.WriteLine("\n🔍 Pesquisando...");
    var response = await assistant.GetTravelRecommendation(query);
    Console.WriteLine($"\n{response}");
}

static async Task HandleItineraryRequest(SmartTourismAssistant assistant)
{
    Console.Write("\n🌆 Para qual destino você quer o roteiro? ");
    var destination = Console.ReadLine();

    Console.Write("📅 Quantos dias de viagem? ");
    if (!int.TryParse(Console.ReadLine(), out int days) || days < 1)
    {
        Console.WriteLine("Número de dias inválido.");
        return;
    }

    Console.Write("🎯 Alguma preferência? (ex: cultura, praia, gastronomia - ou deixe em branco): ");
    var preferences = Console.ReadLine();

    Console.WriteLine("\n📋 Gerando roteiro...");
    var itinerary = await assistant.GenerateTravelItinerary(destination, days, preferences);
    Console.WriteLine($"\n{itinerary}");
}

static async Task HandleComparisonRequest(SmartTourismAssistant assistant)
{
    Console.Write("\n🌍 Primeiro destino para comparar: ");
    var dest1 = Console.ReadLine();

    Console.Write("🌎 Segundo destino para comparar: ");
    var dest2 = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(dest1) || string.IsNullOrWhiteSpace(dest2))
    {
        Console.WriteLine("Destinos inválidos.");
        return;
    }

    Console.WriteLine("\n⚖️ Comparando destinos...");
    var comparison = await assistant.CompareDestinations(dest1, dest2);
    Console.WriteLine($"\n{comparison}");
}