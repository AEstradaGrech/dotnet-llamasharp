namespace DotnetLlamaSharp.LangSearch.Models.Response.RankedSearch
{
    public class RankedDocument
    {
        public int Index { get; set; }
        public string Text { get; set; }
        public float Score { get; set; }
    }
}
