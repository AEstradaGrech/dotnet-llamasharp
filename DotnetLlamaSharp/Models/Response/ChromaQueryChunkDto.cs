namespace DotnetLlamaSharp.Models.Response
{
    public class ChromaQueryChunkDto
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public double Distance { get; set; }
        public string Document { get; set; }
        public List<string> Pages { get; set; }
    }
}
