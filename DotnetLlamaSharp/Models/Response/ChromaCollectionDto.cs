namespace DotnetLlamaSharp.Models.Response
{
    public class ChromaCollectionDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string EmbeddingModel { get; set; }
        public int ChunkDimensions { get; set; }
        public int TotalChunks { get; set; }

    }
}
