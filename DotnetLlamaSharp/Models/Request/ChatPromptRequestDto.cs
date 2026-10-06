using DotnetLlamaSharp.Models.Common;

namespace DotnetLlamaSharp.Models.Request
{
    public class ChatPromptRequestDto : SimplePromptRequestDto
    {
        public List<ChatMessageDto> ChatHistory { get; set; } = new List<ChatMessageDto>();
    }
}
