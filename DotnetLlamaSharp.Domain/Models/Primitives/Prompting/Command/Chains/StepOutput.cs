using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class StepOutput<T> where T : class // JsonStructureOutput
    {
        public StepOutput(T result)
        {
            SerializedResult = JsonSerializer.Serialize<T>(result);
            JsonSchema = JsonSerializer.Serialize(JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(T)));
        }
        public string SerializedResult { get; set; }
        public string JsonSchema { get; set; }

        
    }
}
