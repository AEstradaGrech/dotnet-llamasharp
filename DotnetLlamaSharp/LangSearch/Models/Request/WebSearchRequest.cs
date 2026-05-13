using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.LangSearch.Models.Request
{
    public class WebSearchRequest : LangSearchRequest
    { 
        /// <summary>
        /// Whether to show long text summaries for results. Possible values:
        /// - true: Show summaries.
        /// - false: Do not show summaries (default).
        /// </summary>
        [JsonPropertyName("summary")]
        public bool? Summary { get; set; }

        /// <summary>
        /// Specifies the time range for search results. Possible values:
        ///     - oneDay: Results from the past 24 hours.
        ///     - oneWeek: Results from the past week.
        ///     - oneMonth: Results from the past month.
        ///     - oneYear: Results from the past year.
        ///     - noLimit: No time filter(default).
        /// </summary>
        
        [JsonPropertyName("freshness")]
        public string Freshness { get; set; }
        /// <summary>
        /// The number of results to return. Possible range: 1-10 (default is 10).
        /// </summary>
        [JsonPropertyName("count")]
        public int? Count { get; set; }
    }
}
