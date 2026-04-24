namespace DotnetLlamaSharp.Models.Api.Prompting
{
    public class ChatMessageDto
    {
        public ChatMessageDto() { }
        public ChatMessageDto(string role, string message)
        {
            Role = role;
            Message = message;
        }
        public string Role { get; set; }
        public string Message { get; set; }
    }
}
