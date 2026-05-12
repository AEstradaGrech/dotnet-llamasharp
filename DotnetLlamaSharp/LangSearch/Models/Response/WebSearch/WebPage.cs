using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.LangSearch.Models.Response.WebSearch
{
    public class WebPage
    {
        [JsonPropertyName("webPageUrl")]
        public string Url { get; set; }
        [JsonPropertyName("totalEstimatedMatches")]
        public int? Matches { get; set; }
        [JsonPropertyName("value")]
        public WebPageValue Content { get; set; }

        /// <summary>
        /// Whether some results were removed due to restrictions.
        /// </summary>

        [JsonPropertyName("someResultsRemoved")]
        public bool IsRestricted { get; set; }
    }
}
