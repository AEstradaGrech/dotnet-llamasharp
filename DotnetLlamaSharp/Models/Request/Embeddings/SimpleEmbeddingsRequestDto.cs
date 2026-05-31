namespace DotnetLlamaSharp.Models.Request.Embeddings
{
    /// <summary>
    /// Generate embeddings request for the IEmbeddingsService (Microsoft.AI.Extensions interface)
    /// </summary>
    public class SimpleEmbeddingsRequestDto
    {
        public string? Model { get; set; } = null;
        public int? Dimensions { get; set; } = null;
        public List<string> Texts { get; set; } = new List<string>();
    }
}
