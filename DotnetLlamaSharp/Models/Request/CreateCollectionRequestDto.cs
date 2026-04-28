namespace DotnetLlamaSharp.Models.Request
{
    public class CreateCollectionRequestDto
    {
        public string Name { get; set; }
        public string EmbeddingModel { get; set; }
        public int Dimensions { get; set; }
        public string FileName { get; set; }
        public int ChunkSize { get; set; } = 512;
        public int ChunkOverlap { get; set; } = 50;
        public int InitialSkip { get; set; } = 0;
        public Dictionary<string, object> Metadata { get; set; }
    }
}
