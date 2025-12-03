using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TesteLLMs.Services
{
    public class IntentClassifier
    {
        private readonly OpenAIService _openAI;
        private  List<IntentSample> _samples;

        public IntentClassifier(OpenAIService openAI)
        {
            _openAI = openAI;
            _samples = LoadIntentSamples("Data/intent_dataset.json");
        }

        private List<IntentSample> LoadIntentSamples(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Dataset de intenções não encontrado.");

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<IntentSample>>(json);
        }

        public async Task<string> ClassificarIntencaoAsync(string prompt)
        {
            var promptEmbedding = await _openAI.CreateEmbeddingAsync(prompt);
            
            float maiorSimilaridade = float.MinValue;
            string melhorIntencao = "desconhecida";

            foreach (var sample in _samples)
            {
                float sim = SimilaridadeHelper.CosineSimilarity(promptEmbedding, sample.Embedding);
                if (sim > maiorSimilaridade)
                {
                    maiorSimilaridade = sim;
                    melhorIntencao = sample.Intencao;
                }
            }

            return melhorIntencao;
        }


        public async Task GerarEmbeddingsDeIntencoesAsync()
        {
            var lista = new List<IntentSample>();

            using (var parser = new TextFieldParser("Data/intent_dataset.csv"))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;

                // Pular o cabeçalho
                parser.ReadLine();

                while (!parser.EndOfData)
                {
                    var partes = parser.ReadFields();
                    if (partes == null || partes.Length < 2)
                        continue;

                    var texto = partes[0];
                    var intencao = partes[1];

                    // Gera o embedding (assumindo que seu método já faz isso corretamente)
                    var emb = await _openAI.CreateEmbeddingAsync(texto);

                    lista.Add(new IntentSample
                    {
                        Texto = texto,
                        Intencao = intencao,
                        Embedding = emb
                    });
                }
            }

            var json = JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("Data/intent_dataset.json", json);
        }
    }

    public class IntentSample
    {
        public string Texto { get; set; }
        public string Intencao { get; set; }
        public float[] Embedding { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
