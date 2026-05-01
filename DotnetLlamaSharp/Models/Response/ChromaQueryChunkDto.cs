namespace DotnetLlamaSharp.Models.Response
{
    public class ChromaQueryChunkDto : ChromaChunkDto
    {
        public double Distance { get; set; }
        public string Document { get; set; }
    }
}
