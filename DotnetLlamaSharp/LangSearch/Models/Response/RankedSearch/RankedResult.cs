using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.LangSearch.Models.Response.RankedSearch
{
    public class RankedResult
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }
        [JsonPropertyName("document")]
        public RankedResultDocument Document { get; set; }
        [JsonPropertyName("relevance_score")]
        public float Score { get; set; }
    }
}
