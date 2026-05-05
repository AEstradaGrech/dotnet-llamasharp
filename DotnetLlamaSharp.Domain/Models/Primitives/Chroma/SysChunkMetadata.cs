using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class SysChunkMetadata : ChromaMetadata
    {
        [JsonPropertyName("tag")]               //base-instruction | analysis | Z <-- category / topic
        public string TAG { get; set; }
        [JsonPropertyName("version")]           // prompt variations / iterations
        public string VERSION { get; set; }

    }
}
