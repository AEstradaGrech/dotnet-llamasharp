using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Infrastructure.Models.Inference.StructuredOutputs
{
    public class IntegerChoiceResponse
    {
        [JsonPropertyName("selection")]
        public int Selected { get; set; }
    }
}
