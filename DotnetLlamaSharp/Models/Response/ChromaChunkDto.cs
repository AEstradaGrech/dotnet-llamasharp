namespace DotnetLlamaSharp.Models.Response
{
    public class ChromaChunkDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public ReadOnlyMemory<float> Embedding { get; set; }
        public List<string> DocumentPageIds { get; set; }
    }
}
