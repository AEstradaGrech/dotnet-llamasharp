using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Infrastructure.Extensions.Chroma.Models
{
    public class ChromaClientUpsertRequest
    {
        [JsonPropertyName("ids")]
        public List<string> Ids { get; set; }

        [JsonPropertyName("documents")]
        public List<string>? Documents { get; set; }

        [JsonPropertyName("embeddings")]
        public List<ReadOnlyMemory<float>>? Embeddings { get; set; }

        [JsonPropertyName("metadatas")]
        public List<Dictionary<string, object>>? Metadatas { get; set; }
    }
}
