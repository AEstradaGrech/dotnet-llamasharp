using DotnetLlamaSharp.LangSearch.Models.Response.WebSearch;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.LangSearch.Models.Response
{
    public class LangSearchWebResponse : LangSearchResponse
    {
        
        [JsonPropertyName("data")]
        public SearchData Data { get; set; }
    }
}
