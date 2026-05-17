using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using OllamaSharp.Models.Chat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Schema;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class SingleThrowStep : ChainStep
    {
        public SingleThrowStep() : base() { }

        public SingleThrowStep(StepInstruction instruction, PromptCommandRequest request) : base(request, instruction.FeedFwdInstruction)
        {
            _commands.Add(instruction.Command);
            _request = request;
        }
        public SingleThrowStep(IJsoneable command, PromptCommandRequest request, string? feedFwdInstruction = null) : base(request, feedFwdInstruction)
        {
            _commands.Add(command);
            _request = request;
        }

        public override bool CanBeForged(IChaineable previous)
            => previous == null ? _commands.Count > 0 : !previous.IsMultiSocket;

        public override async Task<ChainResult> ExecuteChainAsync(bool withUserFriendlyMessage = true)
        {
            var firstStep = getFirstStep(current: this);

            var finalStep = await firstStep.Forge(null);

            var typedResult = finalStep.Outputs.First().SerializedResult;

            var finalMessage = new ChatMessage(ChatRole.Assistant.ToString(), string.Empty);

            if (withUserFriendlyMessage)
            {
                var finalizer = ExpandTo<MessagePromptCommand, ChatMessage>(instruction: "Analyze the provided context data from the previous outputs and return the assistant answer (wich is in JSON string format) as a text to keep on chatting",
                    new PromptCommandRequest($"> Original user input: {firstStep.Input}", null, _request.Model));

                var jsonMessage = await finalizer.Forge(finalStep);

                finalMessage = jsonMessage.GetOutputAs<ChatMessage>();
            }

            return new ChainResult(jsonResult: typedResult, finalStep.Outputs.First().JsonSchema, chainInput: firstStep.Input, stepsLog: finalStep.InstructionsLog, processedResult: finalMessage);
        }

        public override async Task<IChaineable> Forge(IChaineable previous)
        {
            await forgeLinkForPlug(previous);
          
            return _next != null ? await _next.Forge(this) : this;
        }
    }
}
