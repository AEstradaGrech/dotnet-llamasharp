using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class ChatChunkMetadata
    {
        [JsonPropertyName("role")]
        public string ROLE { get; set; }
        [JsonPropertyName("actor_name")]
        public string ACTOR_NAME { get; set; }
    }
}
