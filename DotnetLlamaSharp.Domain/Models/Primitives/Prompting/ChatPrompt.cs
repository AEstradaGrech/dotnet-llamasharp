
namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting
{
    public class ChatPrompt
    {
        public string Model { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public List<ChatMessage> ChatHistory { get; set; }
    }
}
