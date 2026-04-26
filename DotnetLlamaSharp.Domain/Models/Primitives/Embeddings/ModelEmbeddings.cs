using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Embeddings
{
    public class ModelEmbeddings
    {
        public string Model { get; set; }
        public List<TextEmbedding> GeneratedEmbeddings { get; set; }
    }
}
