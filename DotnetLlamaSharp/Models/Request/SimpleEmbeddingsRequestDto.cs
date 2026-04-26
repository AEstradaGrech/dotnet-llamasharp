namespace DotnetLlamaSharp.Models.Request
{
    public class SimpleEmbeddingsRequestDto
    {
        public string? Model { get; set; } = null;
        public int? Dimensions { get; set; } = null;
        public List<string> Texts { get; set; } = new List<string>();
    }
}
