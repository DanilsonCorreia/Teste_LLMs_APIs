using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TesteLLMs.Models;

namespace TesteLLMs.Services
{
    public class EmbeddingService
    {
        private readonly DBContext _context;
        private const string VectorIndexName = "IX_EmbeddingsRoteiros_Embedding";
        private const string TableName = "Embeddings";
        private const string VectorColumn = "Embedding";

        public EmbeddingService(DBContext context)
        {
            _context = context;
        }

        public async Task InsertItemsBatchAsync(IEnumerable<Embeddings> items)
        {
            var stopwatch = Stopwatch.StartNew();
            long memoryBefore = GC.GetTotalMemory(true);

            try
            {
                // **Remove índice vetorial antes da inserção**
                await DropVectorIndexIfExistsAsync();

                // Insere os itens
                await _context.Embeddings.AddRangeAsync(items);
                await _context.SaveChangesAsync();

                // Recria o índice vetorial para refletir os novos dados
                await CreateVectorIndexAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro em InsertItemsBatchAsync: {ex.Message}", ex);
            }

            stopwatch.Stop();
            long memoryAfter = GC.GetTotalMemory(false);

            Console.WriteLine($"⏳ SQL INSERT Batch — Tempo: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"💾 SQL INSERT Batch — Memória usada: {memoryAfter - memoryBefore} bytes");
        }

        private async Task DropVectorIndexIfExistsAsync()
        {
            try
            {
                var sqlDrop = $@"
                IF EXISTS (
                    SELECT name
                    FROM sys.indexes
                    WHERE name = '{VectorIndexName}'
                      AND object_id = OBJECT_ID('{TableName}')
                )
                DROP INDEX {VectorIndexName} ON {TableName};";

                await _context.Database.ExecuteSqlRawAsync(sqlDrop);
                Console.WriteLine($"🗑️ Índice vetorial '{VectorIndexName}' removido (se existia).");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        public async Task CreateVectorIndexAsync(int? maxDop = null)
        {
            try
            {
                // Ativa preview features para usar índice vetorial (conforme documentação)  
                var sqlEnablePreview = @"
                    ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON;";

                await _context.Database.ExecuteSqlRawAsync(sqlEnablePreview);

                // Criação de índice vetorial conforme sintaxe do SQL Server 2025 / preview  
                var withParts = new List<string>
                {
                    "METRIC = 'cosine'",
                    "TYPE = 'DiskANN'"
                };
                if (maxDop.HasValue)
                {
                    withParts.Add($"MAXDOP = {maxDop.Value}");
                }

                string withClause = string.Join(", ", withParts);

                var sqlCreate = $@"
                    CREATE VECTOR INDEX {VectorIndexName}
                    ON {TableName} ({VectorColumn})
                    WITH ({withClause});";

                await _context.Database.ExecuteSqlRawAsync(sqlCreate);

                Console.WriteLine($"✅ VECTOR INDEX '{VectorIndexName}' criado com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao criar VECTOR INDEX: {ex.Message}");
                throw;
            }
        }

        // --------------------------------------------------
        // Verifica se um texto já existe (busca “exata” por distância)
        // --------------------------------------------------
        public async Task<(List<Embeddings> Data, BenchmarkMetrics Metrics)> SearchByTypeAsync(
            SqlVector<float> vector,
            string tipo,
            string localizacao,
            int topK = 5)
        {
            var sw = Stopwatch.StartNew();
            long memoryBefore = GC.GetTotalMemory(false);

            try
            {
                var query = _context.Embeddings.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(tipo))
                    query = query.Where(x => x.Type == tipo);

                if (!string.IsNullOrWhiteSpace(localizacao))
                    query = query.Where(x => x.LocationName == localizacao);

                // Usa vector index se disponível: EF.Functions.VectorDistance (distância “exata”)
                query = query.OrderBy(x => EF.Functions.VectorDistance("cosine", x.Embedding, vector));

                var result = await query.Take(topK).ToListAsync();

                sw.Stop();
                long memoryAfter = GC.GetTotalMemory(false);

                var metrics = new BenchmarkMetrics
                {
                    Operation = "SearchByTypeAsync",
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    ElapsedTicks = sw.ElapsedTicks,
                    ResultCount = result.Count,
                    Tipo = tipo,
                    MemoryBeforeBytes = memoryBefore,
                    MemoryAfterBytes = memoryAfter
                };

                Console.WriteLine("===== MÉTRICAS: SearchByTypeAsync =====");
                Console.WriteLine($"Tipo: {metrics.Tipo}");
                Console.WriteLine($"Tempo: {metrics.ElapsedMilliseconds} ms ({metrics.ElapsedTicks} ticks)");
                Console.WriteLine($"Resultados: {metrics.ResultCount}");
                Console.WriteLine($"Memória usada: {metrics.MemoryAfterBytes - metrics.MemoryBeforeBytes} bytes");
                Console.WriteLine("========================================\n");

                return (result, metrics);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro em SearchByTypeAsync: {ex.Message}", ex);
            }
        }

        // --------------------------------------------------
        // BUSCA MULTITIPO paralela — igual ao Qdrant
        // --------------------------------------------------
        public async Task<Dictionary<string, (List<Embeddings> Data, BenchmarkMetrics Metrics)>> SearchRoteiroAsync(
            SqlVector<float> vector,
            IEnumerable<string> tipos,
            string localizacao,
            int topK = 5)
        {
            var tasks = tipos.Select(async tipo =>
            {
                var (data, metrics) = await SearchByTypeAsync(vector, tipo, localizacao, topK);
                return (tipo, data, metrics);
            });

            var results = await Task.WhenAll(tasks);

            return results.ToDictionary(
                x => x.tipo,
                x => ((List<Embeddings>)x.data, x.metrics)
            );
        }

        // --------------------------------------------------
        // Busca aproximada (ANN) usando VECTOR_SEARCH
        // --------------------------------------------------
        public async Task<List<Embeddings>> SearchApproximateAsync(
            float[] queryVector,
            int topN = 5,
            string tipo = null,
            string localizacao = null)
        {
            // Serializa vetor para algo que possa ser usado no SQL
            var json = System.Text.Json.JsonSerializer.Serialize(queryVector);

            var sb = new StringBuilder();
            sb.AppendLine($"DECLARE @qv VECTOR({queryVector.Length}) = '{json}';");
            sb.AppendLine();
            sb.AppendLine("SELECT t.*");
            sb.Append("FROM VECTOR_SEARCH(");
            sb.AppendLine($"    TABLE = {TableName} AS t,");
            sb.AppendLine($"    COLUMN = {VectorColumn},");
            sb.AppendLine($"    SIMILAR_TO = @qv,");
            sb.AppendLine($"    METRIC = 'cosine',");
            sb.AppendLine($"    TOP_N = {topN}");
            sb.AppendLine(") AS s");
            sb.AppendLine($"INNER JOIN {TableName} AS t2 ON t2.[Id] = s.[id]"); // ajuste a PK conforme sua entidade

            if (!string.IsNullOrWhiteSpace(tipo) || !string.IsNullOrWhiteSpace(localizacao))
            {
                sb.Append("WHERE ");
                var filters = new List<string>();
                if (!string.IsNullOrWhiteSpace(tipo))
                    filters.Add($"t2.Tipo = @tipoParam");
                if (!string.IsNullOrWhiteSpace(localizacao))
                    filters.Add($"t2.Localizacao = @locParam");
                sb.Append(string.Join(" AND ", filters));
            }

            var sql = sb.ToString();

            var sqlParams = new List<SqlParameter>();
            if (!string.IsNullOrWhiteSpace(tipo))
                sqlParams.Add(new SqlParameter("@tipoParam", tipo));
            if (!string.IsNullOrWhiteSpace(localizacao))
                sqlParams.Add(new SqlParameter("@locParam", localizacao));

            var resultados = await _context.Embeddings
                .FromSqlRaw(sql, sqlParams.ToArray())   
                .ToListAsync();

            return resultados;
        }
    }
}
