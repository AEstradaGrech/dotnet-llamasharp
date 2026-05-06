using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Infrastructure.Models.Inference.StructuredOutputs
{
    public class BooleanResponse
    {
        [JsonPropertyName("answer")]
        public bool Answer { get; set; }
    }
}
