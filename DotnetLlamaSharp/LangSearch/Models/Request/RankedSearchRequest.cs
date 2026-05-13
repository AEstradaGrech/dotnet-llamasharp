using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.LangSearch.Models.Request
{
    public class RankedSearchRequest : LangSearchRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("top_n")]
        public int? ResultsNumber { get; set; }
        [JsonPropertyName("documents")]
        public List<string> QueriedDocuments { get; set; }
        [JsonPropertyName("return_documents")]
        public bool WithDocuments { get; set; }
    }
}
