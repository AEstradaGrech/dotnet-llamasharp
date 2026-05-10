using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues
{
    public class EnumPromptCommand<TEnum> : ChromaPromptCommand<TEnum> where TEnum : struct, Enum
    {
        public EnumPromptCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) : base(repo, dbMessageName, guidanceMessage, settings)
        {
        }

        public override async Task<TEnum> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            var values = Enum.GetValues<TEnum>().ToList();
            var type = Enum.GetUnderlyingType(typeof(TEnum));

            if (type != typeof(int))
                throw new InvalidOperationException($"{nameof(EnumPromptCommand<TEnum>)} >> invalid ENUM type");

            string systemMessage = await getPromptInstruction();

            foreach (var value in values)
                systemMessage += $"\n{(int)Convert.ChangeType(value, Enum.GetUnderlyingType(typeof(TEnum)))} = {value}";

            var response = await ollama.StructuredPrompt<IntegerChoiceResponse>(request.Prompt, systemMessage);

            return (TEnum)Convert.ChangeType(response.Selected, type);
        }

        protected override string getDefaultInstruction()
            => @"Analyze the provided list of choices and select the integer value / key that matches the best with the user request.
Select ONLY the NUMERIC KEY of the provided Key-Value-Pair list OR NONE if there are no relevant choices for the user intent.
Output your selected choice according to the provided JSON schema.

> CHOICES:";
    }
}
