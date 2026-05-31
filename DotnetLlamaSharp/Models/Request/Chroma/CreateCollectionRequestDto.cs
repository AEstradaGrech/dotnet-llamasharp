namespace DotnetLlamaSharp.Models.Request.Chroma
{
    public class CreateCollectionRequestDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? EmbeddingModel { get; set; }
        public int Dimensions { get; set; }
    }
}
