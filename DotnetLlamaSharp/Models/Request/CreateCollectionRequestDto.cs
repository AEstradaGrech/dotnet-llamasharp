namespace DotnetLlamaSharp.Models.Request
{
    public class CreateCollectionRequestDto
    {
        public string Name { get; set; }
        public string EmbeddingModel { get; set; }
        public string EmbeddingDimensions { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public Dictionary<string, string> MetadataTags { get; set; }
    }
}
