namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class Instruction
    {
        public Instruction(string prompt, string? systemMessage)
        {
            Prompt = prompt;
            SystemMessage = systemMessage;
        }

        public string Prompt { get; set; } // User prompt in chat or 'Actual instruction' (since it seems /generate produces better results when prompting a user request rather than using system message + 'Complete your instruction'
        public string? SystemMessage { get; set; } // maps to Command._systemMessage on instantiation;
    }
}
