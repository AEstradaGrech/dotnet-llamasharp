namespace DotnetLlamaSharp.Models.Response.Chroma
{
    public class ChromaQueryChunkDto : ChromaChunkDto
    {
        public double Distance { get; set; }
        public string Document { get; set; }
    }
}
