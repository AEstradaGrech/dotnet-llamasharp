using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Infrastructure.Extensions.Chroma.Models
{
    public class ChromaClientFilterRequest
    {
        [JsonPropertyName("where")]
        public Dictionary<string, object>? MetadataFilters { get; set; } = null;
        [JsonPropertyName("include")]
        public List<string> Includes { get; set; }
    }
}
