namespace DotnetLlamaSharp.Models.Response.Chroma
{
    public class ChromaFileChunkDto : ChromaChunkDto
    {
        public List<string> DocumentPageIds { get; set; }
    }
}
