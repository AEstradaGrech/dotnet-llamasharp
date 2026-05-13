using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.LangSearch.Models.Request
{
    public class LangSearchRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }
    }
}
