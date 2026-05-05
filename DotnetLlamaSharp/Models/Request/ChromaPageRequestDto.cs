namespace DotnetLlamaSharp.Models.Request
{
    public class ChromaPageRequestDto
    {
        public int PageSize { get; set; }
        public int Page { get; set; }
        public Dictionary<string, object> Filters { get; set; }
        public bool WithEmbeddings { get; set; }
    }
}
