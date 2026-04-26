namespace DotnetLlamaSharp.Models.Common
{
    public class ChatMessageDto
    {
        public ChatMessageDto() { }
        public ChatMessageDto(string role, string message)
        {
            Role = role;
            Content = message;
        }
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
