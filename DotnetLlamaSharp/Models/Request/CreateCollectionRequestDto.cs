namespace DotnetLlamaSharp.Models.Request
{
    public class CreateCollectionRequestDto
    {
        public string Name { get; set; }
        public string EmbeddingModel { get; set; }
        public int Dimensions { get; set; }
    }
}
