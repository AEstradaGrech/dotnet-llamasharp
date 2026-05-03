using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class ChromaCollectionMetadata : ChromaMetadata
    {
        [JsonPropertyName("total_chunks")]
        public int TOTAL_CHUNKS { get; set; }
    }
}
