using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues
{
    public class MultiChoiceCommand : ChromaPromptCommand<List<string>>
    {
        public MultiChoiceCommand() : base() { }
        public MultiChoiceCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) : base(repo, dbMessageName, guidanceMessage, settings)
        {
        }

        public override async Task<List<string>> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            var message = await getPromptInstruction();

            if (request.GetType() != typeof(MultiChoiceRequest))
                throw new InvalidOperationException($"{nameof(MultiChoiceRequest)} >> request is not of TYPE {nameof(MultiChoiceRequest)}");

            var multiChoiceReq = (MultiChoiceRequest) request;

            if (multiChoiceReq.MaxSelections >= multiChoiceReq.Choices.Count)
                throw new InvalidDataException($"{nameof(MultiChoiceCommand)} >> The requested number of selected choices is greater or equal to the available choices");

            foreach (var choice in multiChoiceReq.Choices)
                message += $"\n- {choice}";

            var response = await ollama.StructuredPrompt<MultiChoiceResponse>(request.Prompt, message.Replace("<<MAX_SEL>>",$"{multiChoiceReq.MaxSelections}"), request.Model);

            return response.Selected;
        }
    }
}
