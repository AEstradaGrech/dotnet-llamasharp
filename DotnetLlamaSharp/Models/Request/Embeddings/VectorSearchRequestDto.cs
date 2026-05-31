namespace DotnetLlamaSharp.Models.Request.Embeddings
{
    /// <summary>
    /// Request Dto for the LameChain VectorSearchCommand
    /// </summary>
    public class VectorSearchRequestDto
    {
        public string Index { get; set; }
        public string Query { get; set; }
        public int Dimensions { get; set; }
        public string Model { get; set; }
        public int MaxResults { get; set; }
    }
}
