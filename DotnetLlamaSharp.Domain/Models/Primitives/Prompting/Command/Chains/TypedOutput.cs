using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class TypedOutput<TPrompt> where TPrompt : class // JsonStructureOutput
    {
        public TypedOutput(TPrompt result, string? guidanceMessage = null)
        {
            SerializedResult = JsonSerializer.Serialize<TPrompt>(result);
            JsonSchema = JsonSerializer.Serialize(JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(TPrompt)));
            GuidanceMessage = guidanceMessage;

        }
        public string SerializedResult { get; set; } // THE RESULT AS IT CAME FROM THE LLLM
        public JsonNode JsonSchema { get; set; } // WHAT IS THE RESULT IN LLM TERMS. PASS TO LLM AGAIN TO REPEAT, SERIALIZE AND PASS TO SYSTEM FOR CONTEXT ABOUT PREV STEP
        public string? GuidanceMessage { get; set; } // What to do with the result. This is for the NEXT STEP TOO

        public bool IsValid() => !string.IsNullOrEmpty(SerializedResult) && TypedResult() != null; // Message && Deserializable

        public string SchemaForMessage() => IsValid() ?
            JsonSchema != null ?
            JsonSerializer.Serialize(JsonSchema) :
            JsonSerializer.Serialize(JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(TPrompt))) :
            string.Empty;

        public TPrompt TypedResult() => JsonSerializer.Deserialize<TPrompt>(SerializedResult);
    }
}
