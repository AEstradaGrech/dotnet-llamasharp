using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;

namespace DotnetLlamaSharp.Domain.Models.Request.Prompting
{
    /// <summary>
    /// Extension of the SimplePromptRequest with chat history. Uset when you don't need LameCommands
    /// </summary>
    public class ChatPromptRequest : SimplePromptRequest
    {
        public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
    }
}
