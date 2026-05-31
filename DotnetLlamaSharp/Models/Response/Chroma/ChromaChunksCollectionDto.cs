namespace DotnetLlamaSharp.Models.Response.Chroma
{
    public class ChromaChunksCollectionDto<T> where T : ChromaChunkDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string EmbeddingModel { get; set; }
        public int EmbeddingDimensions { get; set; }
        public int TotalChunks { get; set; }
        public List<T> Chunks { get; set; } = new List<T>();
    }
}
