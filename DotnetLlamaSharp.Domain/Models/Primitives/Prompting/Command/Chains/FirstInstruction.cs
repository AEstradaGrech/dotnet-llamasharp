using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class FirstInstruction : StepInstruction
    {
        public FirstInstruction(IJsoneable command, string userPrompt, string? finalSysmessage, string? chainIntent, string? feedFwd) : base(command, feedFwd) 
        {
            UserPrompt = userPrompt;
            FinalMessageGuidance = finalSysmessage;
            ChainIntent = chainIntent;
        }

        public string UserPrompt { get; set; }
        //The system message to use for the final 'user-friendly' message 
        // if the chain request is 'withFinalUserFriendlyMessage = true'
        // If empty, the final message will be generated using default generalistic hardcoded message
        public string? FinalMessageGuidance { get; set; }
        public string? ChainIntent { get; set; }
    }
}
