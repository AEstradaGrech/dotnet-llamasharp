using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class RagChatCommandRequest : RagCommandRequest
    {
        public string? AgentName { get; set; }
        public string? UserName { get; set; }
        public bool IsNewSession { get; set; }
        public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
    }
}
