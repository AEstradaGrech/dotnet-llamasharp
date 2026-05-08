using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class ScoredChoiceCommand : ChromaPromptCommand<ScoredStringChoice>
    {
        public ScoredChoiceCommand() { }
        public ScoredChoiceCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null) : base(repo, dbMessageName, guidanceMessage)
        {
        }

        public override async Task<ScoredStringChoice> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            var message = await getPromptInstruction();

            if (request.GetType() != typeof(StringChoiceRequest))
                throw new InvalidOperationException($"{nameof(StringChoiceCommand)} >> request is not of TYPE {nameof(StringChoiceRequest)}");

            foreach (var choice in ((StringChoiceRequest)request).Choices)
                message += $"\n- {choice}";

            return await ollama.StructuredPrompt<ScoredStringChoice>(request.Prompt, message, request.Model);
        }
    }
}
