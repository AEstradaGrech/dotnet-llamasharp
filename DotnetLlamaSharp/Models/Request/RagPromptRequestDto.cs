namespace DotnetLlamaSharp.Models.Request
{
    public class RagPromptRequestDto : ChatPromptRequestDto
    {
        public List<string> QueryCollections { get; set; } = new List<string>();
        public int CollectionRetrievals { get; set; } = 1;
        public Dictionary<string, object> EmbeddingFilters { get; set; } = new Dictionary<string, object>();
        public double? MinDistance { get; set; } = null; 
    }
}
