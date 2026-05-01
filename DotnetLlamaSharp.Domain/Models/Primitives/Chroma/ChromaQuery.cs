using DotnetLlamaSharp.Domain.Models.Entities.Chroma;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class ChromaQuery
    {
        public string Query { get; set; }
        public string Collection { get; set; }
        public string EmbeddingModel { get; set; }
        public int Dimensions { get; set; }
        public List<ChromaQueryChunk> Chunks { get; set; }
        public ReadOnlyMemory<float> QueryEmbedding { get; set; }
    }
}
