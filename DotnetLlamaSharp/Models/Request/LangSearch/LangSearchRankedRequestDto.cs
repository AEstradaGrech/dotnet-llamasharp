namespace DotnetLlamaSharp.Models.Request.LangSearch
{
    /// <summary>
    /// Test request. v2 -> LangSearchRanked for multiple sources (for random / general similarity searches)
    /// </summary>
    public class LangSearchRankedRequestDto
    {
        public string Query { get; set; }
        public string? Model { get; set; }
        public int? ResultsNumber { get; set; }
        public bool? WithDocuments { get; set; }
        public List<string> Sources { get; set; }
    }
}
