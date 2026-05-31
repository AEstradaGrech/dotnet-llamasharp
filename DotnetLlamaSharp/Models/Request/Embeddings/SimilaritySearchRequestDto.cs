namespace DotnetLlamaSharp.Models.Request.Embeddings
{
    /// <summary>
    /// Similarity search request to query Chroma Repositories
    /// </summary>
    public class SimilaritySearchRequestDto
    {
        public string Collection { get; set; }
        public string Query { get; set; }
        public int ResultsNumber { get; set; }
        public Dictionary<string, object> MetadataFilters { get; set; } = new Dictionary<string, object>();
    }
}
