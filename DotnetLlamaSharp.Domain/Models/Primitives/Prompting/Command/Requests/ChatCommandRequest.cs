
using OllamaSharp.Models;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class ChatCommandRequest : PromptCommandRequest
    {
        public ChatCommandRequest() { }
        public ChatCommandRequest(bool includeSystem, List<ChatMessage> messages, string message, RequestOptions? settings, string? model = null) : base(message, settings, model)
        {
            ChatHistory = messages;
            IncludeSystemMessage = includeSystem;
        }
        public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
        public bool IncludeSystemMessage { get; set; }
    }
}
