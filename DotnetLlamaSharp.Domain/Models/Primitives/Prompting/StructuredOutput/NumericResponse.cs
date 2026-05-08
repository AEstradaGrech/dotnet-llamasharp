using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput
{
    public class NumericResponse
    {
        [JsonPropertyName("numeric_result")]
        public float? Result { get; set; }
    }
}
