using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput
{
    public class NumericResponse
    {
        [JsonPropertyName("result")]
        public float? Result { get; set; }
    }
}
