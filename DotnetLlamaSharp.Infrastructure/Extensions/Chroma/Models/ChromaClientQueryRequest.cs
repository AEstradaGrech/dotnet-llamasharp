using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Infrastructure.Extensions.Chroma.Models
{
    public class ChromaClientQueryRequest : ChromaClientFilterRequest
    {
        [JsonPropertyName("query_embeddings")]
        public List<ReadOnlyMemory<float>> Embeddings { get; set; }
        [JsonPropertyName("n_results")]
        public int Results { get; set; }
    }
}
