using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class PipedStep : SplitterStep
    {
        public PipedStep(List<StepInstruction> instructions, PromptCommandRequest request, string? feedFwdMessage = null) : base(instructions, request, feedFwdMessage) {}
    }
}
