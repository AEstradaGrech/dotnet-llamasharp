using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.LangSearch.Models.Response.WebSearch
{
    public class QueryContext
    {
        [JsonPropertyName("originalQuery")]
        public string OriginalQuery { get; set; }
    }
}
