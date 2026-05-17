using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class StepInstruction
    {
        public StepInstruction() { }
        public StepInstruction(IJsoneable command, string? feedFwd = null) 
        {
            Command = command;
            FeedFwdInstruction = feedFwd;
        }
        public IJsoneable Command { get; set; }
        public string? FeedFwdInstruction { get; set; }
    }
}
