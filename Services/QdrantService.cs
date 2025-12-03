using Qdrant.Client;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TesteLLMs.Models;

namespace TesteLLMs.Services
{
    public class QdrantService
    {
        private readonly QdrantClient _qdrantClient;

        public QdrantService()
        {
            _qdrantClient = new QdrantClient("localhost", 6334);
        }

        // --------------------------------------------------------------------
        // MÉTRICA — imprime tempo e memória
        // --------------------------------------------------------------------
        private void PrintMetrics(string operacao, Stopwatch stopwatch, long before, long after)
        {
            Console.WriteLine($"⏳ {operacao} — Tempo: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"💾 {operacao} — Memória usada: {after - before} bytes");
            Console.WriteLine("--------------------------------------------------------");
        }

        // ------------------------------
        // Cria coleção se não existir
        // ------------------------------
        public async Task CreateCollectionAsync(string collectionName, ulong vectorSize)
        {
            var stopwatch = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(true);

            var existing = await _qdrantClient.ListCollectionsAsync();

            if (!existing.Contains(collectionName))
            {
                await _qdrantClient.CreateCollectionAsync(collectionName, new VectorParams
                {
                    Size = vectorSize,
                    Distance = Distance.Cosine
                });
            }

            stopwatch.Stop();
            long memAfter = GC.GetTotalMemory(false);
            PrintMetrics("QDRANT CREATE COLLECTION", stopwatch, memBefore, memAfter);
        }

        // ------------------------------
        // Inserir múltiplos itens
        // ------------------------------
        public async Task InsertItemsBatchAsync(string collectionName, IEnumerable<EmbeddingsRoteirosQdrant> items)
        {
            var stopwatch = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(true);

            var points = new List<PointStruct>();

            foreach (var item in items)
            {
                var point = new PointStruct
                {
                    Id = item.Id,
                    Vectors = item.Embedding
                };

                point.Payload.Add("tipo", item.Tipo);
                point.Payload.Add("texto", item.Texto);
                point.Payload.Add("titulo", item.Titulo);
                point.Payload.Add("localizaçao", item.Localizacao);

                points.Add(point);
            }

            await _qdrantClient.UpsertAsync(collectionName, points);

            stopwatch.Stop();
            long memAfter = GC.GetTotalMemory(false);
            PrintMetrics("QDRANT INSERT BATCH", stopwatch, memBefore, memAfter);
        }

        // ------------------------------
        // Busca vetorial com filtro
        // ------------------------------
        public async Task<IReadOnlyList<ScoredPoint>> SearchByTypeAsync(
            string collectionName,
            float[] vector,
            string tipo,
            string localizacao,
            int topK = 5)
        {
            var stopwatch = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(true);

            var filter = new Filter();
            var conditions = new List<Condition>();

            if (!string.IsNullOrEmpty(tipo))
            {
                conditions.Add(new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "tipo",
                        Match = new Match { Keyword = tipo }
                    }
                });
            }

            if (!string.IsNullOrEmpty(localizacao))
            {
                conditions.Add(new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "localizaçao",
                        Match = new Match { Keyword = localizacao }
                    }
                });
            }

            foreach (var c in conditions)
                filter.Must.Add(c);

            var results = await _qdrantClient.SearchAsync(
                collectionName,
                vector.AsMemory(),
                limit: (ulong)topK,
                filter: filter
            );

            stopwatch.Stop();
            long memAfter = GC.GetTotalMemory(false);
            PrintMetrics("QDRANT SEARCH", stopwatch, memBefore, memAfter);

            return results;
        }

        // ------------------------------
        // Busca paralela para múltiplos tipos
        // ------------------------------
        public async Task<Dictionary<string, IReadOnlyList<ScoredPoint>>> SearchRoteiroAsync(
            string collectionName,
            float[] vector,
            IEnumerable<string> tipos,
            string localizacao,
            int topK = 5)
        {
            var stopwatch = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(true);

            var tasks = tipos.Select(async tipo =>
            {
                var res = await SearchByTypeAsync(collectionName, vector, tipo, localizacao, topK);
                return (tipo, res);
            }).ToList();

            var results = await Task.WhenAll(tasks);

            stopwatch.Stop();
            long memAfter = GC.GetTotalMemory(false);
            PrintMetrics("QDRANT MULTI-SEARCH", stopwatch, memBefore, memAfter);

            return results.ToDictionary(r => r.tipo, r => r.res);
        }

        // ------------------------------
        // Verificação de texto duplicado
        // ------------------------------
        public async Task<bool> ExisteTextoAsync(string collectionName, string texto)
        {
            var stopwatch = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(true);

            var filter = new Filter();
            filter.Must.Add(new Condition
            {
                Field = new FieldCondition
                {
                    Key = "texto",
                    Match = new Match { Keyword = texto }
                }
            });

            var result = await _qdrantClient.ScrollAsync(collectionName, filter: filter, limit: 1);

            stopwatch.Stop();
            long memAfter = GC.GetTotalMemory(false);
            PrintMetrics("QDRANT EXISTS CHECK", stopwatch, memBefore, memAfter);

            return result.Result?.ToList().Count > 0;
        }
    }
}
