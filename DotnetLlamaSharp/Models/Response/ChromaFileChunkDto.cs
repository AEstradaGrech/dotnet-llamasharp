namespace DotnetLlamaSharp.Models.Response
{
    public class ChromaFileChunkDto : ChromaChunkDto
    {
        public List<string> DocumentPageIds { get; set; }
    }
}
