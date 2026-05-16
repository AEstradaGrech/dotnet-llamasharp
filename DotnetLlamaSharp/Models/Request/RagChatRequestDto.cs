using DotnetLlamaSharp.Models.Common;
using System.ComponentModel;

namespace DotnetLlamaSharp.Models.Request
{
    public class RagChatRequestDto : RagPromptRequestDto
    {
        public string AgentName { get; set; }
        public string UserName { get; set; }
        public List<ChatMessageDto> ChatHistory { get; set; }
    }
}
