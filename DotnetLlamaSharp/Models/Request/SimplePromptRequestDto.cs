namespace DotnetLlamaSharp.Models.Request
{
    public class SimplePromptRequestDto
    {
        public string Prompt { get; set; }
        public string? SystemMessage { get; set; } = null;
        public string? Model { get; set; } = null;
        public int MaxTokens { get; set; } = 600;
        public float Temperature { get; set; } = 0.7f;
    }
}
