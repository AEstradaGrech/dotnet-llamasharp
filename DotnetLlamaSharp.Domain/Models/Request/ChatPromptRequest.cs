using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class ChatPromptRequest : SimplePromptRequest
    {
        public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
    }
}
