using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput
{
    public class ScoredBoolResponse : BooleanResponse
    {
        [JsonPropertyName("confidence_score")]
        [JsonRequired]
        public float Score { get; set; }
        [JsonPropertyName("justification")]
        [JsonRequired]
        public string Justification { get; set; }
    }
}
