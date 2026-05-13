using Dotnet.LangSearch.SDK.Models.Enums;

namespace DotnetLlamaSharp.Models.Request
{
    public class LangSearchRankedPageRequestDto
    {
        public string Query { get; set; }
        public string? RankingModel { get; set; }
        public int Count { get; set; }
        public EQueryFreshness Freshness { get; set; }
        public float? ScoreThreshold { get; set; }
    }
}
