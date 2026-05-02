using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class ChatPromptRequest
    {
        public string Prompt { get; set; }
        public string SystemMessage { get; set; } = string.Empty;
        public string? Model { get; set; } = null;
        public int MaxTokens { get; set; } = 600;  
        public float Temperature { get; set; } = 0.7f;
        public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
    }
}
