using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.LangSearch.Models.Response.WebSearch
{
    public class WebPage
    {
        [JsonPropertyName("webSearchUrl")]
        public string Url { get; set; }
        [JsonPropertyName("totalEstimatedMatches")]
        public int? Matches { get; set; }
        [JsonPropertyName("value")]
        public List<WebPageValue> Results { get; set; }

        /// <summary>
        /// Whether some results were removed due to restrictions.
        /// </summary>

        [JsonPropertyName("someResultsRemoved")]
        public bool IsRestricted { get; set; }
    }
}
