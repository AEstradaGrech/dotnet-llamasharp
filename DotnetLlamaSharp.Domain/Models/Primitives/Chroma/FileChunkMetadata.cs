using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class FileChunkMetadata : ChromaMetadata
    {
        [JsonPropertyName("pages")]
        public string PAGES { get; set; } = "";
        [JsonPropertyName("file_extension")]
        public string FILE_EXTENSION { get; set; }
        [JsonPropertyName("topic")]
        public string TOPICS { get; set; }
    }
}
