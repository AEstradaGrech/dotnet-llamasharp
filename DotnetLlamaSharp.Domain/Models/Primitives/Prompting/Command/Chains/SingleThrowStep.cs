using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class SingleThrowStep : ChainStep
    {
        public SingleThrowStep() : base() { }

        public SingleThrowStep(StepInstruction instruction, ChainPromptRequest request) : base(request, instruction.FeedFwdInstruction)
        {
            _commands.Add(instruction.Command);
        }
        public SingleThrowStep(IJsoneable command, ChainPromptRequest request, string? feedFwdInstruction = null) : base(request, feedFwdInstruction)
        {
            _commands.Add(command);

        }

        /// <summary>
        /// This is the constructor for the FIRST RUNNER. ALWAYS

        /// It recieves and stores the original (and unique-per-chain) ChainRunner or a Clone (if swapped in a ThrowTo(parallelSubchain) [in splitters | pipes])
        /// 
        /// > If it is the FIRST RUNNER, the ChainRunner MUST CONTAIN the general INIT DATA (user prompt, chain intent and finalSysMessage) to 
        /// be used as contextual info on each step. Also...
        /// 
        /// > Subscribes to the NECESSARY events to persist outputs and messages across the chain steps. This is the only place for a FIRST RUNNER to do that, the 
        /// other runners must catch the previous pass on runtime (calling Forge(previous)) to do this and keep running the chain.
        /// 
        /// > The UserPrompt is prompted ONLY in the first step (the rest should work on interpretations of the previous data and 
        /// feedFwd guidance messages)
        /// 
        /// > If it is the FIRST SUBRUNNER, it works exactly as any other step in terms of recieved data / template instruction formation, but
        ///   it recieves a clone of the ChainRunner to run their chain in parallel and then return their ChainRunner clone with the results
        ///   to append  them to the (unique) original ChainRunner and keep running the main chain.
        ///   
        ///     *Note: subrunners MUST use Link(prev, fwd: false, two-way-bind: false) to recieve a previous from the main chain
        /// 
        /// > FIRST RUNNER is differenced from a FIRST SUB_RUNNER because the total RunnedInstruction stored in the ChainRunner & SwappedClones
        /// 
        /// </summary>
        /// <param name="command"> The initial chain command</param>
        /// <param name="runner">A data structure containing the initial data and store the generated data to pass it along the chain</param>
        /// <param name="feedFwdInstruction">This is like 'what you want to tell the next one about the instruction output' </param>
        public SingleThrowStep(IJsoneable command, ChainRunner runner, string? feedFwdInstruction = null) 
            : base()
        {
            //validateRunner.UserInput ONLY (the rest are optional)

            _request = new ChainPromptRequest(); // just to avoid crashes since the FIRST RUNNER does not recieve a previous guidance message (stored in the previous output and passed with the _request)
            _commands.Add(command);
            _feedForwardInstruction = feedFwdInstruction;
            _runner = runner;
            _passCatchTimestamp = DateTime.Now;
           
            onRunNotify += _runner.OnRunnerNotification; // write stuff to runner. This is always triggered AFTER forgeLink or when appending subchain results
           
            onSupportIncoming += _runner.OnSupporterResponse;
            //_runner.onSupportRequest += OnRunnerCall;
            // onSupportRunner += _runner.OnRunnedSupport;
            _runner.onReplayRequest += SendReplay; // handle request of writing stuff to runner. This might be called at the end of the chain to debug or whatever TBD
            
        }
        // All runners MUST check if they are in possession of the ChainRunner (IsRunner) and... (<step-type-check>)
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

            ChainRunner finalRunner = null;
            if (withUserFriendlyMessage)
            {
                var finalizer = ExpandTo<MessagePromptCommand, ChatMessage>(instruction: "Analyze the provided context data from the previous outputs and return the assistant answer (which is in JSON string format) as a text to keep on chatting",
                    new ChainPromptRequest($"> Original user input: {firstStep.Input}", finalStep.Runner.FwdSystemMessage, null, _request.Model));

                var jsonMessage = await finalizer.Forge(finalStep);

                finalMessage = jsonMessage.GetOutputAs<ChatMessage>();

                finalRunner = jsonMessage.Drop();
            }

            else finalRunner = finalStep.Drop();

            return new ChainResult(finalRunner.ForgedLogs, jsonResult: typedResult, finalStep.Outputs.First().JsonSchema, chainInput: firstStep.Input, stepsLog: finalRunner.RunnedInstructions, processedResult: finalMessage);
        }

        public override async Task<IChaineable> Forge(IChaineable previous)
        {
            if (!IsFirstStep())
            {
                if(!IsRunning && !hasCatchedThrow(previous))
                    throw new InvalidOperationException($"{nameof(SingleThrowStep)} >> {nameof(Forge)} >> {_commands.First().GetType().Name} >> {nameof(hasCatchedThrow)} >> AN ERROR HAS OCCURED WHILE PASSING THE CHAIN RUNNER FROM PREVIOUS STEP");
            }

            else _passCatchTimestamp = DateTime.Now;

            await forgeLinkForPlug(previous);

            submitForgeLog();

            return _next != null ? await _next.Forge(this) : this;
        }
    }
}
