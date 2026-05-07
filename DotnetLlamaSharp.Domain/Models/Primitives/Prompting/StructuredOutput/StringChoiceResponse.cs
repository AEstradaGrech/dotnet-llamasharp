using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput
{
    public class StringChoiceResponse
    {
        [JsonPropertyName("selection")]
        public string Selected { get; set;  }
    }
}
