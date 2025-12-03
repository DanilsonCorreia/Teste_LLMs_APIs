using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Runtime;
using System.Diagnostics;
using TesteLLMs.Models;
using TesteLLMs.Services;

// CONFIGURAÇÃO
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

// HOST + DI
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // --------------------------
        // 🔥 Registrar DBContext
        // --------------------------
        services.AddDbContext<DBContext>(options =>
             options.UseSqlServer(
                 config.GetConnectionString("DefaultConnection")
             )
             //.LogTo(Console.WriteLine, new[] { DbLoggerCategory.Model.Name, DbLoggerCategory.Database.Name }, LogLevel.Debug)
             //.EnableSensitiveDataLogging() // Cuidado com dados sensíveis em produção
         );

        // --------------------------
        // 🔥 Registrar serviços COM parâmetros
        // --------------------------
        services.AddSingleton<OpenAIService>(sp =>
            new OpenAIService(config["OpenAIKey"]));

        services.AddSingleton<AnthropicService>(sp =>
            new AnthropicService(config["AnthropicKey"]));

        services.AddSingleton<CohereService>(sp =>
            new CohereService(config["CohereKey"]));

        // --------------------------
        // 🔥 Registrar serviços SEM parâmetros
        // --------------------------
        services.AddScoped<GenerateEmbedings>();
        services.AddScoped<EmbeddingService>();
        services.AddScoped<QdrantService>();
        services.AddScoped<IntentClassifierQdrant>();
    })
    .Build();
// Carregar serviços
//var destinationsFilePath = config["DestinationsFilePath"];
//var openAI = config["OpenAIKey"];
//var gemini = config["GeminiKey"];
//var anthropic = new AnthropicService(config["AnthropicKey"]);
//var cohere = new CohereService(config["CohereKey"]);


//var service = new OpenAIService(openAI);
////var classifier = new IntentClassifier(service);
//var qdrantService = new QdrantService();
//var classifierQdrant = new IntentClassifierQdrant(service, qdrantService);
//var generateEmbeddings = new GenerateEmbedings(service, qdrantService);

//string prompt = "quero viajar para a madeira quais os pontos mais interessantes?";
//var stopwatch = Stopwatch.StartNew();
//string intencao = await classifier.ClassificarIntencaoAsync(prompt);

//stopwatch.Stop();

//Console.WriteLine($"🧭 Intenção detectada: {intencao}");
//Console.WriteLine($"⏱️ Tempo de execução: {stopwatch.Elapsed.TotalSeconds:F2} segundos");

//Console.WriteLine("🧩 Gerando embeddings e populando o Qdrant...");
//await classifierQdrant.GerarEmbeddingsDeIntencoesAsync();

//Console.WriteLine("✅ Embeddings inseridos com sucesso no Qdrant!");
//await classifierQdrant.GerarEmbeddingsDeIntencoesAsync();
//string poisFilePath = @"C:\Users\Danilson Correia\source\repos\TesteLLMs\Data\POIS.xlsx";
//await generateEmbeddings.GerarEmbeddingsDePontosTuristicosAsync(poisFilePath, "pois");

//string rotasFilePath = @"C:\Users\Danilson Correia\source\repos\TesteLLMs\Data\Rotas.xlsx";
//await generateEmbeddings.GerarEmbeddingsDePontosTuristicosAsync(rotasFilePath, "rotas");
using var scope = host.Services.CreateScope();
var embeding = scope.ServiceProvider.GetRequiredService<EmbeddingService>();

var openaiService = scope.ServiceProvider.GetRequiredService<OpenAIService>();
var qdrantService = scope.ServiceProvider.GetRequiredService<QdrantService>();
var generate = scope.ServiceProvider.GetRequiredService<GenerateEmbedings>();
//var exist = await embeding.ExisteTextoAsync("Corrida 1. Corrida 1 Localizado em Macedo de Cavaleiros");

string eventosFilePath = @"C:\Users\Danilson Correia\source\repos\TesteLLMs\Data\Eventos.xlsx";
//var generate = scope.ServiceProvider.GetRequiredService<GenerateEmbedings>();

await generate.GerarEmbeddingsDePontosTuristicosAsync(eventosFilePath, "eventos");

//Console.WriteLine("Processo concluído!");

//Console.WriteLine("💬 Simulador de Chat com Classificação de Intenções");
//await embeding.CreateVectorIndexAsync();
Console.WriteLine("Digite 'sair' para encerrar.\n");

while (true)
{
    Console.Write("🧍 Você: ");
    string prompt = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(prompt))
        continue;

    if (prompt.Equals("sair", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("👋 Encerrando conversa...");
        break;
    }

    try
    {
        var stopwatch = Stopwatch.StartNew();

        // Classifica a intenção
        var dados = new DadosIntencao(); /*await openaiService.GetIntentFromPrompt(prompt)*/;

        var emb = await openaiService.CreateEmbeddingAsync(prompt);

        dados.Intencao = "eventos";
        //dados.Localizacao = "Bragança";

        //Console.WriteLine($"🤖 Intenção detectada: {dados.Intencao}");
        //Console.WriteLine("-------------------------------------\n");
        //Console.WriteLine("Lista de pesquisas por Embeddings:\n");

        if (dados.Intencao == "roteiro")
        {
            string[] tipos = { "pois", "rotas", "eventos" };

            // Busca todas as categorias de uma vez
            var dadosRoteiro = await embeding.SearchRoteiroAsync(new SqlVector<float>(emb),  tipos, dados.Localizacao, 5);

            // Monta o contexto textual já formatado
            string contexto = string.Join("\n\n", dadosRoteiro.Select(kv =>
            {
                var tituloPontos = kv.Value.Data
                    .Select(p => p.Title)
                    .Where(t => !string.IsNullOrEmpty(t));

                return $"🧭 {kv.Key.ToUpper()}:\n" + string.Join("\n", tituloPontos);
            }));
            Console.WriteLine($"Metrics: \n{dadosRoteiro}");
            // Exemplo de envio ao GPT
            Console.WriteLine("🤖 Assistente: ");
            await openaiService.CriarRoteiroStreaming(prompt, contexto, chunk =>
            {
                Console.Write(chunk); // imprime cada pedaço conforme chega
            });

            Console.WriteLine("\n-------------------------------------\n");
        }
        else if (dados.Intencao == "poi")
        {
            string contexto = string.Join("\n",
                (await qdrantService.SearchByTypeAsync("dados_roteiro", emb, "pois", dados.Localizacao, 5))
                    .Select(p => p.Payload.ContainsKey("titulo") ? p.Payload["titulo"].StringValue : ""));

            var resposta = await openaiService.CriarRoteiro(prompt, contexto);
            Console.WriteLine(resposta);
        }
        else if (dados.Intencao == "rotas")
        {
            // Busca as rotas no Qdrant
            string contexto = string.Join("\n",
                (await qdrantService.SearchByTypeAsync("dados_roteiro", emb, "rotas", dados.Localizacao, 5))
                    .Select(p => p.Payload.ContainsKey("titulo") ? p.Payload["titulo"].StringValue : "")
            );

            // Cria o prompt com o contexto
            var resposta = await openaiService.CriarRoteiro(prompt, contexto);

            // Mostra no console
            Console.WriteLine(resposta);
        }
        else if (dados.Intencao == "eventos")
        {
            var dadosRoteiro = await embeding.SearchByTypeAsync(new SqlVector<float>(emb), "eventos", dados.Localizacao, 5);
            var dadosRoteiroQdrant = await qdrantService.SearchByTypeAsync("dados_roteiro", emb, "eventos", dados.Localizacao, 5);

            // Busca os eventos no Qdrant
            string contexto = string.Join("\n",
                (dadosRoteiro.Data
                    .Select(p => p.Title +"Localização:"+ p.LocationName)
            ));

            // Gera o roteiro com base no contexto de eventos
            await openaiService.CriarRoteiroStreaming(prompt, contexto, chunk =>
            {
                Console.Write(chunk); // imprime cada pedaço conforme chega
            });
        }
        else
        {
            Console.WriteLine($"Este é um assistente de turismo.");
        }
        stopwatch.Stop();
        Console.WriteLine("-------------------------------------\n");
        Console.WriteLine($"⏱️ Tempo de execução: {stopwatch.Elapsed.TotalSeconds:F2} segundos");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Erro: {ex.Message}");
    }
}

