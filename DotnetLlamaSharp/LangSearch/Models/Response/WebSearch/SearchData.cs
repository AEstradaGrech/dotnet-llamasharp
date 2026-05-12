using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.LangSearch.Models.Response.WebSearch
{
    public class SearchData
    {
        [JsonPropertyName("queryContext")]
        public QueryContext QueryContext { get; set; }
        [JsonPropertyName("webPages")]
        public List<WebPage> WebPages { get; set; }
    }
}
