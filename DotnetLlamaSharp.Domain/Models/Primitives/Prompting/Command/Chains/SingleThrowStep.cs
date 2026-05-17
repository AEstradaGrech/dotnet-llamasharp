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
        public ChainLink OutputLink => _outputs.FirstOrDefault();
        public IJsoneable Command => _commands.FirstOrDefault();

        public SingleThrowStep() : base() { }

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
            if (!CanBeForged(previous))
                throw new InvalidOperationException($"{nameof(SingleThrowStep)} >> {nameof(Forge)} >> An error has occured whil forging the chain link. No Commands found for the current step");
            // --------------------- protected abstract Task ForgeLink(previous) --> SingleThrow = esto hasta _output=, Splitter -> foreach Branch Task.Create -> ForgeLink(prev) + add output
            if (!IsFirstStep() && previous.IsForged)
            {
                // The guidance message is appended to the command final system message in this way: _systemMessage (optional. guidance. on constructor) + dbMessage (optional. main msg. on construction) | defaultInstruction(optional. main msg. hardcoded) + _request.Guidance
                _request.GuidanceMessage = guidanceMessageFrom(_request.Prompt, previous.PromptedInstruction, previous.Outputs.First().SerializedResult, previous.Outputs.First().SchemaForMessage(), previous.Outputs.First().GuidanceMessage); 

                _instructionsLog.AddRange(previous.InstructionsLog);
            }

            await forgeLink();
          
            return _next != null ? await _next.Forge(this) : this;
        }

        protected async Task forgeLink()
        {
            var jsonResult = await Command.JsonPrompt(_request, returnFullInstruction: false, preInstruction: preInstructionTag); //Skip GuidanceMessage, return only instruction for this step

            _promptedInstruction = jsonResult.Instruction;

            _instructionsLog.Add(_promptedInstruction);

            _outputs.Add(new ChainLink(_id, jsonResult.RawJson, JsonSerializerOptions.Default.GetJsonSchemaAsNode(jsonResult.Type), jsonResult.Type, feedForwardMessage: _feedForwardInstruction));
        }
        protected string guidanceMessageFrom(string prompt, string instruction, string serialized, string jsonSchema, string? outputGuidance = null)
         => @$"
# CONTEXT: A previous process on your current task has reported the next results for this user query and instruction, 
use this information if you find it relevant for your current task.

- PREVIOUS PROMPT: {prompt}
- PREVIOUS TASK: {instruction.Replace(preInstructionTag, "").Trim()}

- PREVIOUS OUTPUT:

{serialized}

- PREVIOUS OUTPUT SCHEMA:

{jsonSchema}

{(string.IsNullOrEmpty(outputGuidance) ? string.Empty : $"# PREVIOUS STEP REQUEST (this is what you are expected to do with the previous output data): {outputGuidance}")}";
    }
}
