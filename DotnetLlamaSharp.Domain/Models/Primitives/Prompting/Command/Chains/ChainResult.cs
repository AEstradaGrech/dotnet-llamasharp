using OllamaSharp.Models.Chat;
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
            Message = null;
            SystemInstructions = new ChatMessage(ChatRole.System.ToString(), string.Empty);
        }

        public ChainResult(string jsonResult, JsonNode resultSchema, string chainInput, List<string> stepsLog, ChatMessage? processedResult = null) : this(jsonResult, resultSchema, chainInput) 
        {
            ChainStepsLog = stepsLog;
            Message = processedResult;
            
            if(stepsLog.Count > 0)
                for (int i = 0; i < stepsLog.Count; i++)
                    SystemInstructions.Content += $"- STEP {i + 1} {stepsLog[i]}";
        }

        public ChatMessage? Message { get; set; }
        public ChatMessage SystemInstructions { get; set; }
        public string Json { get; set; }
        public JsonNode Schema { get; set; }
        public string InputPrompt { get; set; }
        public List<string> ChainStepsLog { get; set; }

    }
}
