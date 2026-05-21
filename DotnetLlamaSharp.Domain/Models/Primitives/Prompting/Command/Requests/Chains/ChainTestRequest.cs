using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains;
using DotnetLlamaSharp.Domain.Models.Request;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests.Chains
{
    public class ChainTestRequest : SimpleCommandRequest
    {

        public bool WithReport { get; set; }
        public bool WithFinalMessage { get; set; }
        public string? FinalSystemMessage { get; set; } = string.Empty;
        public CommandSettings? FinalMessageSettings { get; set; } = null; // ?? Default (Input)
        
        public List<Instruction> Instructions { get; set; } = new List<Instruction>();
    }
}
