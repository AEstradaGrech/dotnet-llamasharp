using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class ChromaMetadata
    {
        [JsonPropertyName("text")]
        public string TEXT { get; set; }
        [JsonPropertyName("model")]
        public string MODEL { get; set; }
        [JsonPropertyName("dimensions")]
        public int DIMENSIONS { get; set; }
        [JsonPropertyName("document")]
        public string DOCUMENT { get; set; }

    }
}
