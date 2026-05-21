using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Request;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public static class FluentChainExtensions
    {
        public static SingleThrowStep StartWith(FirstInstruction firstInstruction, CommandSettings defaultSettings)
            => Activator.CreateInstance(typeof(SingleThrowStep), 
                    firstInstruction.Command, 
                    new ChainRunner(firstInstruction.UserPrompt, defaultSettings, firstInstruction.FinalMessageGuidance, firstInstruction.ChainIntent), 
                    firstInstruction.FeedFwdInstruction) 
                as SingleThrowStep;
        
        public static SingleThrowStep Then(this SingleThrowStep step, IJsoneable command, ChainPromptRequest request, string? feedFwdInstruction = null)
        {
            /*
                BASIC API:
                    - Then(cmd) = 1 x inpt / 1 x opt --> Appends every prev link as it is to the sysmsg (not cxt-len safe)
                    - Tap([]) = splits the chain by plugging-in N commands that will run in parallel and generates N output links
                    - Split(cmd, [cmds]) = similar to Tap, but runs first a desired command then fans-out the result to the plugged commands and generates 1 x Plugged Output
                    - Pipe(cmd) = OnForge, executes the command on each prev out link. It is a SplitterStep extension. Generates 1 x prevOut.Num LINKS
                    - Join(cmd) = OnForge, generates a single output using the prevLinks and a Joining Command. This is a 'controlled' merge
                    - Feed<TJoiner>([cmds]) = OnForge executes every cmd with the prevOutput and generates 1 SINGLE output with it. Is a 1 step Split & Join
                    - Feed<TJoiner>(piped, [cmds]) = OnForge executes every cmd with the prevOutput, executes the piped cmd on every splitter cmd and joins the result with the TJoiner generating 1 SINGLE output with it. Is a 1 step Split & Pipe & Join
                    - Loop(times: N, cmd) do N times the input command with the prev output
                    - Branch(A, B, decisorCmd) --> on runtime evaluates decision and forges(A) or (B) SWAPPING the step AND CONTINUEING
                   
                                 ------B`4 ---
                                |  ,-- B`2 ---|
                               FEED --- B`3 - THEN --(b)--        
                                |              |         |
                       |- B  -- B` --- B" -----|         |
                    A  -  C  -- C` --- C" ---- D --(a)-- E -- OPT 
                 START   TAP  PIPE   PIPE    JOIN   THEN          
             */

            var nextStep = step.ExpandTo(command, request, feedFwdInstruction);

            step.Link(nextStep, isForward: true, isTwoWay: true);

            return nextStep;
        }

        public static ChainStep ThenExecute(this SingleThrowStep step, out ChainResult result, bool withFinalMessage = true)
        {
            result = step.ExecuteChainAsync(withFinalMessage)
                .ConfigureAwait(continueOnCapturedContext: false)
                .GetAwaiter()
                .GetResult();

            return step;
        }

        // Expose ChainSteps to FeedAlsoFrom([Guid]) to pass chain results past the
        // Next step
        public static SingleThrowStep ExposeThisId(this SingleThrowStep step, out Guid id)
        {
            id = step.Id;

            return step;
        }
        public static SplitterStep ExposeThisId(this SplitterStep step, out Guid id)
        {
            id = step.Id;

            return step;
        }

        /// <summary>
        /// Splits the chain in N branches, 1 x instruction
        /// Executes every command using the same SINGLE PREV OUTPUT, passing a unique feedForwardMessage for each plugged instruction.
        /// GENERATES 1 x PLUGGED INSTRUCTION OUTPUT LINKS
        /// </summary>
        /// <param name="step"></param>
        /// <param name="instructions"></param>
        /// <param name="stepRequest"></param>
        /// <returns></returns>
        public static SplitterStep Tap(this SingleThrowStep step, List<StepInstruction> instructions, ChainPromptRequest? stepRequest = null)
        {
            var split = step.Plug(instructions, stepRequest);

            step.Link(split, isForward: true, isTwoWay: true);

            return split;
        }

        public static SplitterStep SplitThrough(this SingleThrowStep step, StepInstruction splitted, List<StepInstruction> instructions, ChainPromptRequest? stepRequest = null)
        {
            var split = step.SplitTo(splitted, instructions, stepRequest);

            step.Link(split, isForward: true, isTwoWay: true);

            return split;
        }

        /// <summary>
        /// Feeds a specified command with the list of passed instructions
        /// that will execute their command using the SINGLE PREV OUTPUT LINK
        /// to produce N outputs to be added to the feeded command system message
        /// </summary>
        /// <param name="step"></param>
        /// <param name="instruction"></param>
        /// <param name="instructions"></param>
        /// <param name="stepRequest"></param>
        /// <returns></returns>
        public static SingleThrowStep Feed(this SingleThrowStep step, StepInstruction instruction, List<StepInstruction> instructions, ChainPromptRequest? stepRequest = null)
        {
            var split = step.Plug(instructions, stepRequest);

            step.Link(split, isForward: true, isTwoWay: true);

            var feeded = split.ExpandTo<JunctionStep>(instruction.Command, stepRequest, instruction.FeedFwdInstruction);

            split.Link(feeded, isForward: true, isTwoWay: true);

            return feeded;
        }

        /// <summary>
        /// Runs the piped command for each PREVIOUS OUTPUT LINK, using the same feedForward message for all the results
        /// GENERATES 1 OUTPUT LINK x PREVIOUS OUTPUT LINK
        /// </summary>
        /// <param name="step"></param>
        /// <param name="command"></param>
        /// <param name="pipeFeedFwd"></param>
        /// <returns></returns>
        public static PipedStep Pipe(this SplitterStep step, IJsoneable command, string? pipeFeedFwd = null, ChainPromptRequest? stepRequest = null)
        {
            var split = step.ExpandTo<PipedStep>(new StepInstruction(command, pipeFeedFwd), stepRequest);

            step.Link(split, isForward: true, isTwoWay: true);

            return split;
        }
        
        public static SingleThrowStep Join( this SplitterStep step, StepInstruction instruction, ChainPromptRequest? stepRequest = null)
        {
            var next = step.ExpandTo<JunctionStep>(instruction.Command, stepRequest, instruction.FeedFwdInstruction);
            
            step.Link(next, isForward: true, isTwoWay: true);
            
            return next;
        }

        /// <summary>
        /// Injects a random / uncontrolled amount of context data (with optional guidance) in the system message of 
        /// the step at runtime
        /// 
        /// SingleThrow Chain Adapter
        /// </summary>
        /// <param name="step"></param>
        /// <param name="sources"></param>
        /// <param name="guidance"></param>
        /// <returns></returns>
        public static SingleThrowStep WithRebujito(this SingleThrowStep step, List<string> sources, string? guidance = null, int? feedDose = null)
        {
            if (feedDose != null)
                step.BoostWith(sources.Select(source => feedDose != null && feedDose > 0 && source.Length > feedDose ? source.Substring(0, feedDose.Value) : source).ToList(), guidance);

            else step.BoostWith(sources, guidance);

            return step;
        }

        /// <summary>
        /// Injects a random / uncontrolled amount of context data (with optional guidance) in the system message of 
        /// the step at runtime
        /// 
        /// Splitter Chain Adapter
        /// </summary>
        /// <param name="step"></param>
        /// <param name="sources"></param>
        /// <param name="guidance"></param>
        /// <returns></returns>
        public static SplitterStep WithRebujito(this SplitterStep step, List<string> sources, string? guidance = null, int? feedDose = null)
        {
            if(feedDose != null)
                step.BoostWith(sources.Select(source => feedDose != null && feedDose > 0 && source.Length > feedDose ? source.Substring(0, feedDose.Value) : source).ToList(), guidance);

            else step.BoostWith(sources, guidance);
            
            return step;
        }
        // Sequence (N, returnMaxTokens: 1000) --> opt = input * N & collect? (for N -> opt = N * input) equivalente a loop esto es .For() con el collect incorporado
        //  > usado con limite puede usarse para algo asi como MapAndReduce <- se añade SIEMPRE un eslabón de sumarizacion con MaxToken = param. Util para controlar CTX_LEN
        //
        // Switch (N, evaluator) --> opt = input * N + best scored eval 
        // 
        // .WithRelay(prompteable, voltageCondition) <- se activa si true, si no bypass
        // .AddCapacitor(1k tokens) <- si el sysmessage llega a valor de carga lo mantiene (con sumarizaciones o algo asi)
        // .Choke<TVal>(3 validations) where TVal : JsonValidatorCommand  <-- Request.CommandValidations + ChokeVals?
        public static async Task<ChainResult> ThenExecuteAsync(this SingleThrowStep step, bool withFinalMessage = true, bool withReplay = false)
            => await step.ExecuteChainAsync(withFinalMessage, withReplay);
       
        public static ChainStep Then<TCommand, TResult>(this ChainStep step, string? instruction, ChainPromptRequest? stepRequest = null, string? feedFwdInstruction = null) 
            where TCommand : BasePromptCommand<TResult>, new() 
            where TResult : class
        {
            var nextStep = step.ExpandTo<TCommand, TResult>(instruction, stepRequest);

            step.Link(nextStep, isForward: true, isTwoWay: true);

            return nextStep as ChainStep;
        }

        //public static ChainStep Then<TCommand, TResult>(this ChainStep step, PromptCommandRequest request,  PromptCommandRequest  commandRequest)
        //{
        //    //step.From<TCommand, typeof(PromptCommandRequest), 

        //    return step;
        //}
        public static TypedOutput<TResult> Then<TResult>(this TypedOutput<TResult> step, IPrompteable<TResult> cmd /*IPromptCommand<TResult> cmd*/, ChainPromptRequest? stepRequest = null) where TResult : class
        {
            
            return step;
            //return nextStep;
        }
        //public static ChainStep LoadNext<TCommand>(this ChainStep step)
        //{

        //    return step;
        //}

        //public static ChatMessage ThenFinish(this ChainStep step)
        //{
        //    if (step.Command.GetType() == typeof(MessagePromptCommand)) return step.Output.TypedResult() as ChatMessage;

        //    return null;
        //}
    }
}
