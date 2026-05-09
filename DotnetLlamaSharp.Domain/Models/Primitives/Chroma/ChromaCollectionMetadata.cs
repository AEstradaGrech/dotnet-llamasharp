using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class ChromaCollectionMetadata : ChromaMetadata
    {
        [JsonPropertyName("total_chunks")]
        public int TOTAL_CHUNKS { get; set; }
        [JsonPropertyName("chunk_type")]
        public int CHUNK_TYPE { get; set; }
    }
}
