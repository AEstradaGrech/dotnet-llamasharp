namespace DotnetLlamaSharp.LangSearch.Models.Response.RankedSearch
{
    public class RankedData
    {
        public string Query { get; set; }
        public string Model { get; set; }
        public List<RankedDocument> Results { get; set; }
    }
}
