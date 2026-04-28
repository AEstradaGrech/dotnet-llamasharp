namespace DotnetLlamaSharp.Models.Request
{
    public class CreateCollectionRequestDto
    {
        public string Name { get; set; }
        public int EmbeddingDimensions { get; set; } = 512;
        public string FileName { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
