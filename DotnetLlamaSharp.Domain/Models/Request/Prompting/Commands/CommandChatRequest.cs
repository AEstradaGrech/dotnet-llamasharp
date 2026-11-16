
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;

namespace DotnetLlamaSharp.Domain.Models.Request.Prompting.Commands
{
    /// <summary>
    /// Chat version of the SimpleCommandRequest to be used with Lame Commands
    /// </summary>
    public class CommandChatRequest : SimpleCommandRequest
    {
        public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
    }
}
