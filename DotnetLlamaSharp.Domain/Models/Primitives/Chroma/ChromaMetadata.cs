using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class ChromaMetadata
    {
        [JsonPropertyName("model")]
        public string MODEL { get; set; }
        [JsonPropertyName("dimensions")]
        public int DIMENSIONS { get; set; }
        [JsonPropertyName("document_name")]
        public string DOCUMENT_NAME { get; set; }

    }
}
