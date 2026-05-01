using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class ChromaCollectionMetadata : ChromaMetadata
    {
        [JsonPropertyName("chunks")]
        public int CHUNKS { get; set; }
    }
}
