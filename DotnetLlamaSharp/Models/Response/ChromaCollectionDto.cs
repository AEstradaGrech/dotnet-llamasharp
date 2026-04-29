namespace DotnetLlamaSharp.Models.Response
{
    public class ChromaCollectionDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string EmbeddingModel { get; set; }
        public int EmbeddingDimensions { get; set; }
        public int TotalChunks { get; set; }
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public List<string> Files { get; set; } = new List<string>();

    }
}
