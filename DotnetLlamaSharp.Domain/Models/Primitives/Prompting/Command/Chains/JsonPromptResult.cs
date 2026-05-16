using System.Text.Json.Nodes;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class JsonPromptResult
    {
        public JsonPromptResult() { }
        public JsonPromptResult(string instruction, object result, Type type, string jsonString, JsonNode schemaNode)
        {
            Instruction = instruction;
            Object = result;
            Type = type;
            RawJson = jsonString;
            Schema = schemaNode;
        }
        public string Instruction { get; set; }
        public Type Type { get; set; }
        public object Object { get; set; }
        public string RawJson { get; set; }
        public JsonNode Schema { get; set; }
    }
}
