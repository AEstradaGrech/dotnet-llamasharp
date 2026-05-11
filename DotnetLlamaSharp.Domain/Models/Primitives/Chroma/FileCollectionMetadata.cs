using Dotnet.Chroma.Repositories.Models.Metadata;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class FileCollectionMetadata : ChromaCollectionMetadata
    {
        [JsonPropertyName("files")]
        public string FILES { get; set; } = "";

        [JsonPropertyName("chunk_sizes")]
        public string CHUNK_SIZES { get; set; } = "";
        [JsonPropertyName("chunk_overlaps")]
        public string CHUNK_OVERLAPS { get; set; } = "";

        [JsonPropertyName("skipped_pages")]
        public string SKIPPED_PAGES { get; set; } = "";
        [JsonPropertyName("page_cutoffs")]
        public string PAGE_CUTOFFS { get; set; } = "";
        [JsonPropertyName("pages")]
        public string PAGES { get; set; } = "";
        [JsonPropertyName("topics")]
        public string TOPICS { get; set; } = "";
    }
}
