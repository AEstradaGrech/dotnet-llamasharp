using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class EnumPromptCommand<TEnum> : DbPromptCommand<TEnum> where TEnum : struct, Enum
    {
        public EnumPromptCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null) : base(repo, dbMessageName, guidanceMessage)
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
    }
}
