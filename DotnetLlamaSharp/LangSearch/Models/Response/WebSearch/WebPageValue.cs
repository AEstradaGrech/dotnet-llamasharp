using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.LangSearch.Models.Response.WebSearch
{
    public class WebPageValue
    {
        /// <summary>
        /// Unique identifier for the web page.
        /// Return value example: https://api.langsearch.com/v1/web-search#1
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The title of the webpage.
        /// Return value example: ESG Report June 2024 - Apple Inc. (AAPL)
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// The URL of the webpage.
        /// Return value example: https://www.crispidea.com/report/esg-report-june-2024-apple/
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// The title of the webpage.
        /// Return value example: https://www.crispidea.com/report/esg-report-june-2024-apple/
        /// </summary>
        [JsonPropertyName("displayUrl")]
        public string DisplayUrl { get; set; }

        /// <summary>
        /// A brief snippet from the web page. 
        /// Returns a long text.
        /// </summary>
        [JsonPropertyName("snippet")]
        public string Snippet { get; set; }
        /// <summary>
        /// Full summary (request option)
        /// </summary>
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        /// <summary>
        /// The date the page was published.
        /// </summary>
        [JsonPropertyName("datePublished")]
        public DateTime? PublishedDate { get; set; }

        /// <summary>
        /// The last date the page was crawled.
        /// </summary>
        [JsonPropertyName("dateLastCrawl")]
        public DateTime? LastCrawledDate { get; set; }
    }
}
