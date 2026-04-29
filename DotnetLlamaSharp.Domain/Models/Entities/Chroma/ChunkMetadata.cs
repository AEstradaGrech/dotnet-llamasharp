using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChunkMetadata : ChromaMetadata
    {
        [JsonPropertyName("document")]
        public string DOCUMENT { get; set; }

        [JsonPropertyName("pages")]
        public List<int> PAGES { get; set; }


    }
}
