using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput
{
    public class BooleanResponse
    {
        [JsonPropertyName("answer")]
        public bool Answer { get; set; }
    }
}

