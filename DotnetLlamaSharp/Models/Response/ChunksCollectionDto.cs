namespace DotnetLlamaSharp.Models.Response
{
    public class ChunksCollectionDto : ChromaCollectionDto
    {
        public List<ChromaChunkDto> Chunks { get; set; }
    }
}
