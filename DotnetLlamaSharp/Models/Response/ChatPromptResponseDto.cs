using DotnetLlamaSharp.Models.Common;

namespace DotnetLlamaSharp.Models.Response
{
    public class ChatPromptResponseDto
    {
        public string Model { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public List<ChatMessageDto> ChatHistory { get; set; } = new List<ChatMessageDto>();
    }
}
