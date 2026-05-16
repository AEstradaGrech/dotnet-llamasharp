using System.Text.Json.Nodes;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class ChainResult
    {
        public ChainResult() { }
        public ChainResult(string jsonResult, JsonNode resultSchema, string chainInput)
        {
            Json = jsonResult;
            Schema = resultSchema;
            InputPrompt = chainInput;
        }

        public ChainResult(string jsonResult, JsonNode resultSchema, string chainInput, ChatMessage? processedResult = null) : this(jsonResult, resultSchema, chainInput) { Message = processedResult; }

        public ChatMessage? Message { get; set; }
        public string Json { get; set; }
        public JsonNode Schema { get; set; }
        public string InputPrompt { get; set; }
    }
}
