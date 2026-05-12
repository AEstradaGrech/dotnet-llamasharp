using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.LangSearch.Models.Response
{
    public class LangSearchRankedResponse : LangSearchResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
    }
}
