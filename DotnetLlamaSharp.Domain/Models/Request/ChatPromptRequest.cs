using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class ChatPromptRequest : SimplePromptRequest
    {
        public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
    }
}
