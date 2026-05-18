using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class SingleThrowStep : ChainStep
    {
        public SingleThrowStep() : base() { }

        public SingleThrowStep(StepInstruction instruction, PromptCommandRequest request) : base(request, instruction.FeedFwdInstruction)
        {
            _commands.Add(instruction.Command);
        }
        public SingleThrowStep(IJsoneable command, PromptCommandRequest request, string? feedFwdInstruction = null) : base(request, feedFwdInstruction)
        {
            _commands.Add(command);

        }
        public SingleThrowStep(IJsoneable command, ChainRunner runner, PromptCommandRequest request, string? feedFwdInstruction = null) 
            : this(command, request, feedFwdInstruction)
        {
            _runner = runner;
            _passCatchTimestamp = DateTime.Now;
            //SUBSCRIBE TO EVENTS. THIS IS THE ONLY PLACE TO SETUP FOR CHAIN FIRST_STEPS. THE REST HAVE TO CATCH THE PASS ON FORGE

            onRunNotify += _runner.OnRunnerNotification; // write stuff to runner. This is triggered AFTER forgeLink(); PERSIST FWD INSTRUCTIONS FOR MORE THAN ONE STEP
            _runner.onReplayRequest += SendReplay; // handle request of writing stuff to runner. This might be called at the end of the chain to debug or whatever TBD

        }
        // Tiene el balon Y ...
        public override bool CanBeForged(IChaineable previous)
            => IsRunning && previous == null ? _commands.Count > 0 : !previous.IsMultiSocket;

        public override async Task<ChainResult> ExecuteChainAsync(bool withUserFriendlyMessage = true)
        {
            var firstStep = getFirstStep(current: this);

            // onPassRunner(this, _runner)
            // next -- subscribed -> OnReceive(IChaineable, previous, runner) _runner = runner --> IsRunning = true
            var finalStep = await firstStep.Forge(null);

            var typedResult = finalStep.Outputs.First().SerializedResult;

            var finalMessage = new ChatMessage(ChatRole.Assistant.ToString(), string.Empty);

            if (withUserFriendlyMessage)
            {
                var finalizer = ExpandTo<MessagePromptCommand, ChatMessage>(instruction: "Analyze the provided context data from the previous outputs and return the assistant answer (which is in JSON string format) as a text to keep on chatting",
                    new PromptCommandRequest($"> Original user input: {firstStep.Input}", null, _request.Model));

                var jsonMessage = await finalizer.Forge(finalStep);

                finalMessage = jsonMessage.GetOutputAs<ChatMessage>();
            }

            return new ChainResult(jsonResult: typedResult, finalStep.Outputs.First().JsonSchema, chainInput: firstStep.Input, stepsLog: finalStep.InstructionsLog, processedResult: finalMessage);
        }

        public override async Task<IChaineable> Forge(IChaineable previous)
        {
            if (!IsFirstStep())
            {
                if(!IsRunning && !hasCatchedPass(previous))
                    throw new InvalidOperationException($"{nameof(SingleThrowStep)} >> {nameof(Forge)} >> {_commands.First().GetType().Name} >> {nameof(hasCatchedPass)} >> AN ERROR HAS OCCURED WHILE PASSING THE CHAIN RUNNER FROM PREVIOUS STEP");
            }

            else _passCatchTimestamp = DateTime.Now;
            
            await forgeLinkForPlug(previous);

            submitForgeLog();

            return _next != null ? await _next.Forge(this) : this;
        }
    }
}
