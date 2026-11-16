namespace DotnetLlamaSharp.Models.Response.Chroma
{
    public class EmbeddingsResponseDto
    {
        public string Model { get; set; }
        public int Dimensions { get; set; }
        public List<ReadOnlyMemory<float>> Embeddings { get; set; }
    }
}
