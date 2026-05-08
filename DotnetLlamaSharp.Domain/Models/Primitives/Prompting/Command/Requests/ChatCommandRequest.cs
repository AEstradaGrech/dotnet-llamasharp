
namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class ChatCommandRequest : PromptCommandRequest
    {
        public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
    }
}
