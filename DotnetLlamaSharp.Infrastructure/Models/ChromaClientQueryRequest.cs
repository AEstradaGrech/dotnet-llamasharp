using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Infrastructure.Models
{
    public class ChromaClientQueryRequest
    {
        [JsonPropertyName("query_embeddings")]
        public List<ReadOnlyMemory<float>> Embeddings { get; set; }
        [JsonPropertyName("n_results")]
        public int Results { get; set; }
        [JsonPropertyName("where")]
        public Dictionary<string, object> MetadataFilters { get; set; }
    }
}
