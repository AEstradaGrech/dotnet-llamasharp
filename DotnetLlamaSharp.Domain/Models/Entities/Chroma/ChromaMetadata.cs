using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaMetadata
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("dimensions")]
        public int Dimensions { get; set; }
    }
}
