using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Infrastructure.Extensions.Chroma.Models
{
    public class ChromaClientGetRequest : ChromaClientFilterRequest
    {
        [JsonPropertyName("ids")]
        public List<string>? Ids { get; set; } = null;

        [JsonPropertyName("limit")]
        public int? Results { get; set; } = null;

        [JsonPropertyName("offset")]
        public int? Skip { get; set; } = null;
    }
}
