namespace DotnetLlamaSharp.Models.Response.Chroma
{
    public class ChromaFilesCollectionDto : ChromaChunksCollectionDto<ChromaFileChunkDto>
    {
        public int Pages { get; set; }
        public List<string> SkippedPages { get; set; }
        public List<string> PageCutoffs { get; set; }
        public List<string> ChunkSizes { get; set; }
        public List<string> ChunkOverlaps { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Files { get; set; } = new List<string>();

    }
}
