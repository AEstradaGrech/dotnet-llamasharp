using DotnetLlamaSharp.LangSearch.Models.Enums;

namespace DotnetLlamaSharp.Models.Request
{
    public class LangSearchWebSearchDto
    {
        public string Query { get; set; }
        public int Count { get; set; }
        public EQueryFreshness Freshness { get; set; }
        public bool? WithSummary { get; set; }
    }
}
