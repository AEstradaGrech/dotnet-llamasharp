using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class ChatCollectionMetadata : ChromaCollectionMetadata
    {
        [JsonPropertyName("total_sessions")]
        public int TOTAL_SESSIONS { get; set; }
        [JsonPropertyName("session_ids")]
        public string SESSION_IDS { get; set; } // CONCAT(SELECT ID FROM collection WHERE metadata = chat_init, ",")
        [JsonPropertyName("current_session_id")]
        public string CURRENT_SESSION_ID { get; set; }
        [JsonPropertyName("current_session_chunks")]
        public int CURRENT_SESSION_CHUNKS { get; set; }

    }
}
