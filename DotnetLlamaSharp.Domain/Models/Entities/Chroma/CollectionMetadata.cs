using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class CollectionMetadata : ChromaMetadata
    {
        [JsonPropertyName("files")]
        public List<string> Files { get; set; }

        [JsonPropertyName("chunks")]
        public int Chunks { get; set; }

        [JsonPropertyName("chunk_size")]
        public int ChunkSize { get; set; }
        [JsonPropertyName("chunk_overlap")]
        public int ChunkOverlap { get; set; }

    }
}
