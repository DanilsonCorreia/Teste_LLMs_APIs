using ExcelDataReader; // Adicione este using
using Microsoft.Data.SqlTypes;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Data; // Adicione este using
using System.IO;
using System.Linq;
using System.Text; // Adicione este using para Encoding.RegisterProvider
using System.Threading.Tasks;
using TesteLLMs.Models;

namespace TesteLLMs.Services
{
    public class GenerateEmbedings
    {
        private readonly OpenAIService _openAI;
        private readonly QdrantService _qdrant;
        private const string _collectionName = "dados_roteiro";
        private readonly EmbeddingService _embeddingsRoteiros;

        public GenerateEmbedings(OpenAIService openAI, QdrantService qdrant, EmbeddingService embeddingService)
        {
            _openAI = openAI;
            _qdrant = qdrant;
            _embeddingsRoteiros = embeddingService;

            // REMOVA A CONFIGURAÇÃO DE LICENÇA DO EPPLUS, pois não estamos mais usando EPPlus aqui.
            // ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.Commercial;
        }

        public async Task GerarEmbeddingsDePontosTuristicosAsync(string filePath, string tipo)
        {
            var itemsQdrant = new List<EmbeddingsRoteirosQdrant>();
            var items = new List<Embeddings>();



            // Verificar se o arquivo existe
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Arquivo não encontrado: {filePath}");
                return;
            }

            try
            {
                // Registro do provedor de codificação para evitar problemas com alguns arquivos Excel
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // Configurar a leitura para usar a primeira linha como cabeçalho
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });

                    // Pegar a primeira tabela (primeira worksheet)
                    DataTable dataTable = result.Tables[0];

                    var rowCount = dataTable.Rows.Count;
                    Console.WriteLine($"Encontradas {rowCount} linhas na worksheet");

                    // Itera sobre as linhas da tabela (a primeira linha é o cabeçalho, já tratada por UseHeaderRow = true)
                    for (int i = 0; i < rowCount; i++)
                    {
                        DataRow row = dataTable.Rows[i];

                        // Obtém os valores usando os nomes das colunas do cabeçalho
                        // Assegure-se de que os nomes das colunas "Title", "Description", "Location"
                        // correspondem exatamente aos cabeçalhos do seu arquivo Excel.
                        var title = row["Title"]?.ToString()?.Trim() ?? "";
                        var description = row["Description"]?.ToString()?.Trim() ?? "";
                        var location = row["LocationName"]?.ToString()?.Trim() ?? "";

                        // --- INÍCIO DA VERIFICAÇÃO MELHORADA PARA PULAR REGISTROS INVÁLIDOS ---
                        // Pular registros onde Title, Description ou Location são nulos/vazios
                        if (string.IsNullOrWhiteSpace(title) || title.ToUpper() == "NULL" || title.ToUpper() == "TITLE")
                        {
                            Console.WriteLine($"Pulando linha {i + 2}: Título inválido ou vazio ('{title}')."); // +2 para corresponder à linha do Excel (1 cabeçalho, +1 para índice 0)
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(description) || description.ToUpper() == "NULL")
                        {
                            Console.WriteLine($"Pulando linha {i + 2}: Descrição inválida ou vazia para '{title}'.");
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(location) || location.ToUpper() == "NULL")
                        {
                            Console.WriteLine($"Pulando linha {i + 2}: Localização inválida ou vazia para '{title}'.");
                            continue;
                        }
                        // --- FIM DA VERIFICAÇÃO MELHORADA ---

                        // Combinar título e descrição para criar o texto do embedding
                        var texto = $"{title}. {description} Localizado em {location}";

                        try
                        {
                            var embedding = await _openAI.CreateEmbeddingAsync(texto);

                            itemsQdrant.Add(new EmbeddingsRoteirosQdrant
                            {
                                Id = new PointId(Guid.NewGuid()),
                                Tipo = tipo,
                                Texto = texto,
                                Embedding = embedding,
                                Localizacao= location,
                                Titulo = title
                                
                                
                            });

                            items.Add(new Embeddings
                            {
                                EmbGuid = Guid.NewGuid(),
                                Type = tipo,
                                Text = texto,
                                Embedding = new SqlVector<float>(embedding),
                                LocationName= location,
                                Title = title
                            });

                            Console.WriteLine($"Processado: {title}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erro ao processar '{title}': {ex.Message}");
                        }
                    }
                }

                await ProcessarEInserirItems(itemsQdrant, items);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar arquivo Excel: {ex.Message}");
            }
        }

        private async Task ProcessarEInserirItems(List<EmbeddingsRoteirosQdrant> itemsQdrant, List<Embeddings> items)
        {
            if (items.Any() && itemsQdrant.Any())
            {
                var embeddingDimension = (ulong)items.First().Embedding.Length;

                // Verificar se a coleção já existe antes de criar
                try
                {
                    await _qdrant.CreateCollectionAsync(_collectionName, embeddingDimension);
                    Console.WriteLine($"Coleção '{_collectionName}' criada com dimensão {embeddingDimension}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Coleção já existe ou erro na criação: {ex.Message}");
                }

                // Inserir embeddings em lotes para melhor performance
                var batchSize = 100;
                for (int i = 0; i < items.Count; i += batchSize)
                {
                    var batchQdrant = itemsQdrant.Skip(i).Take(batchSize);
                    var batch = items.Skip(i).Take(batchSize);
                    await _qdrant.InsertItemsBatchAsync(_collectionName, batchQdrant);
                    await _embeddingsRoteiros.InsertItemsBatchAsync(batch);
                    Console.WriteLine($"Inserido lote {i / batchSize + 1} de {(items.Count + batchSize - 1) / batchSize}");
                }

                Console.WriteLine($"Processo concluído! {items.Count} pontos turísticos foram indexados.");
            }
            else
            {
                Console.WriteLine("Nenhum item foi processado.");
            }
        }
    }
}