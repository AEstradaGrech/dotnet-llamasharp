using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    //METADATA.DOCUMENT = SESSION_CHUNK_ID
    public class ChatChunkMetadata : ChromaMetadata
    {
        [JsonPropertyName("total_messages")]
        public int TotalMessages { get; set; }
        [JsonPropertyName("current")]
        public bool IsCurrent { get; set; }
        [JsonPropertyName("llm_model")]
        public string InferenceModel { get; set; } //This is just informative

    }
}
