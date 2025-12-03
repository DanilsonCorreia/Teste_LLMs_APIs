using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tensorflow;

namespace TesteLLMs.Services
{
    internal class IntentClassifierQdrant
    {
        private readonly OpenAIService _openAI;
        private readonly QdrantService _qdrant;
        private readonly string _collectionName = "intencoes";

        public IntentClassifierQdrant(OpenAIService openAI, QdrantService qdrant)
        {
            _openAI = openAI;
            _qdrant = qdrant;
        }

        // Gera embeddings e insere no Qdrant
        // ------------------------------
        public async Task GerarEmbeddingsDeIntencoesAsync()
        {
            var items = new List<EmbeddingItem>();

            using (var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser("Data/intent_dataset.csv"))
            {
                parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                parser.ReadLine(); // Cabeçalho

                while (!parser.EndOfData)
                {
                    var partes = parser.ReadFields();
                    if (partes == null || partes.Length < 2) continue;

                    var texto = partes[0];
                    var intencao = partes[1];

                    var emb = await _openAI.CreateEmbeddingAsync(texto);

                    items.Add(new EmbeddingItem
                    {
                        Id = new PointId(Guid.NewGuid()),
                        Texto = texto,
                        Tipo = intencao,
                        Embedding = emb
                    });
                }
            }

            // Criar coleção (caso não exista)
            await _qdrant.CreateCollectionAsync(_collectionName, (ulong)items.First().Embedding.Length);

            // Inserir embeddings
            //await _qdrant.InsertItemsBatchAsync(_collectionName, items);
        }

        public async Task AtualizarIntencoesNoQdrantAsync()
        {
            const string collectionName = "intencoes";

            using var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser("Data/intent_dataset.csv");
            parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;
            parser.ReadLine(); // cabeçalho

            var novosItens = new List<EmbeddingItem>();
            var todosTextos = new List<string>();

            while (!parser.EndOfData)
            {
                var partes = parser.ReadFields();
                if (partes == null || partes.Length < 2) continue;

                var texto = partes[0];
                var intencao = partes[1];
                todosTextos.Add(texto);

                // Verifica se já existe esse texto na base
                bool existe = await _qdrant.ExisteTextoAsync(collectionName, texto);
                if (!existe)
                {
                    var emb = await _openAI.CreateEmbeddingAsync(texto);
                    var id = Convert.ToBase64String(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(texto)));

                    novosItens.Add(new EmbeddingItem
                    {
                        Id = new PointId(Guid.NewGuid()),
                        Texto = texto,
                        Tipo = intencao,
                        Embedding = emb
                    });
                }
            }

            if (novosItens.Any())
            {
                Console.WriteLine($"➕ Inserindo {novosItens.Count} novos itens...");
                //await _qdrant.InsertItemsBatchAsync(collectionName, novosItens);
            }
            else
            {
                Console.WriteLine("✅ Nenhum novo item encontrado.");
            }
        }

        // ------------------------------
        // Classifica a intenção com base no embedding e Qdrant
        // ------------------------------
        //public async Task<string> ClassificarIntencaoAsync(float[] emb)
        //{
            

        //    // Busca no Qdrant a intenção mais próxima
        //    //var result = await _qdrant.SearchByTypeAsync(_collectionName, emb.ToArray(),"intencoes", topK: 1);

        //    if (result.Count == 0)
        //        return "desconhecida";

        //    var best = result.First();
        //    return best.Payload["tipo"].StringValue;
        //}
    }

    // ------------------------------
    // Estrutura do item de intenção
    // ------------------------------
    public class EmbeddingItem
    {
        public PointId Id { get; set; }
        public string Texto { get; set; }
        public string Tipo { get; set; }
        public float[] Embedding { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}

