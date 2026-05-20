using System.Text.Json;
using System.Text.Json.Nodes;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    // A: pasar tipos
    // B: pasar nodo y nameof(Type) //  Type type = ass.GetType(typename); ass = LameChain.SDK.Models (los tipos derivados de usuarios cascarian [loadFrom: result.GetType().AssemblyQualifiedName, typeName = result.GetType.Name]
    public class ChainLink // JsonStructureOutput
    {
        public ChainLink(Guid stepId, string instruction, string jsonResult, JsonNode result, Type serializedType, string? feedForwardMessage = null)
        {
            StepId = stepId;
            Instruction = instruction;
            SerializedResult = jsonResult;
            JsonSchema = result;
            GuidanceMessage = feedForwardMessage;
            SerializedType = serializedType;
        }
        public Guid StepId { get; set; }
        public string Instruction { get; set; }
        public string SerializedResult { get; set; } // THE RESULT TAL CUAL
        public JsonNode JsonSchema { get; set; } // WHAT IS THE RESULT IN LLM TERMS. PASS TO LLM AGAIN TO REPEAT, SERIALIZE AND PASS TO SYSTEM FOR CONTEXT ABOUT PREV STEP
        public string? GuidanceMessage { get; set;  } // What to do with the result. This is for the NEXT STEP TOO
        public Type SerializedType { get; set; }
        public bool IsValid() => !string.IsNullOrEmpty(SerializedResult) && JsonSchema != null; // Message && Deserializable
        
        public string SchemaForMessage() => IsValid() ? 
            JsonSerializer.Serialize(JsonSchema) : 
            "[CONTEXT ERROR: MISSING DATA]"; //Tell the LLM there is an error in this section so it does not hallucinate the response because it is instructed to review an empty section
        
        public object TypedResult() => JsonSerializer.Deserialize(SerializedResult, SerializedType) ?? throw new InvalidOperationException($"Failed to deserialize JSON to type {SerializedType.Name}");
    }
}
