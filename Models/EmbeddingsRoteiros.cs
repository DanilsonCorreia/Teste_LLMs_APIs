using Microsoft.Data.SqlTypes;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TesteLLMs.Models
{
    public class EmbeddingsRoteirosQdrant
    {
        public PointId Id { get; set; }
        public string Texto { get; set; }
        public string Tipo { get; set; }
        public string Titulo { get; set; }
        public string Localizacao { get; set; }
        public float[] Embedding { get; set; }
        
    }
    public class EmbeddingsRoteiros
    {
        public Guid Id { get; set; }
        public string Texto { get; set; }
        public string Tipo { get; set; }
        public string Titulo { get; set; }
        public string Localizacao { get; set; }
        public SqlVector<float> Embedding { get; set; }
    }

    public class Embeddings
    {
        public int Id { get; set; }
        public Guid EmbGuid { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string LocationName { get; set; }
        public SqlVector<float> Embedding { get; set; }
    }
}
