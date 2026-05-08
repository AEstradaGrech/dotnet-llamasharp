using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput
{
    public class ScoredStringChoice : StringChoiceResponse
    {
        [JsonPropertyName("confidence_score")]
        [JsonRequired]
        public float Score { get; set; }
        [JsonPropertyName("justification")]
        [JsonRequired]
        public string Justification { get; set; }
    }
}
