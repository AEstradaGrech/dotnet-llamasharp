using Dotnet.Chroma.Repositories.Models.Metadata;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    //METADATA.DOCUMENT = SESSION_CHUNK_ID
    public class ChatChunkMetadata : ChromaMetadata
    {
        [JsonPropertyName("total_messages")]
        public int TOTAL_MESSAGES { get; set; }
        [JsonPropertyName("chat_init")]
        public bool CHAT_INIT { get; set; }
        [JsonPropertyName("current")]
        public bool CURRENT { get; set; }
        [JsonPropertyName("llm_model")]
        public string LLM_MODEL { get; set; } //This is just informative
        [JsonPropertyName("session_id")]
        public string SESSION_ID { get; set; }
        [JsonPropertyName("session_chunks")]
        public int SESSION_CHUNKS { get; set; }

    }
}
