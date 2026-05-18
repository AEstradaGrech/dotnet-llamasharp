using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Request;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public static class FluentChainExtensions
    {
        public static SingleThrowStep StartWith(IJsoneable command, PromptCommandRequest request, string? feedFwdInstruction = null)
            => Activator.CreateInstance(typeof(SingleThrowStep), command, request, feedFwdInstruction) as SingleThrowStep;
        
        public static SingleThrowStep Then(this SingleThrowStep step, IJsoneable command, PromptCommandRequest request, string? feedFwdInstruction = null)
        {
            /*
                BASIC API:
                    - Then(cmd) = 1 x inpt / 1 x opt --> Appends every prev link as it is to the sysmsg (not cxt-len safe)
                    - Split([cmds]) = OnForge, executes every cmd with the previous opt, that should be always a single. Generates 1 x cmds.Num LINKS
                    - Pipe(cmd) = OnForge, executes the command on each prev out link. It is a SplitterStep extension. Generates 1 x prevOut.Num LINKS
                    - Join(cmd) = OnForge, generates a single output using the prevLinks and a Joining Command. This is a 'controlled' merge
                    - Feed<TJoiner>([cmds]) = OnForge executes every cmd with the prevOutput and generates 1 SINGLE output with it. Is a 1 step Split & Join
                    - Feed<TJoiner>(piped, [cmds]) = OnForge executes every cmd with the prevOutput, executes the piped cmd on every splitter cmd and joins the result with the TJoiner generating 1 SINGLE output with it. Is a 1 step Split & Pipe & Join
                    - And<TJunction>(a, b) OnForge executes a and B and GENERATES 1 OPT LINK USING the TJunction & the opt from A & B as input. It is a Split & Join step. It is Plug<TJoiner>([]) for 2 conditions (synthactic sugar)
                    - Loop(times: N, cmd) do N times the input command with the prev output
                    - Branch(A, B, decisorCmd) --> on runtime evaluates decision and forges(A) or (B) SWAPPING the step AND CONTINUEING
                   
             */

            var nextStep = step.ExpandTo(command, request);

            step.Link(nextStep, isForward: true, isTwoWay: true); // feedForwardData?

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


        /// <summary>
        /// Splits the chain in N branches, 1 x instruction
        /// Executes every command using the same SINGLE PREV OUTPUT, passing a unique feedForwardMessage for each plugged instruction.
        /// GENERATES 1 x PLUGGED INSTRUCTION OUTPUT LINKS
        /// </summary>
        /// <param name="step"></param>
        /// <param name="instructions"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static SplitterStep Tap(this SingleThrowStep step, List<StepInstruction> instructions, PromptCommandRequest request)
        {
            var split = step.Plug(instructions, request);

            step.Link(split, isForward: true, isTwoWay: true);

            return split;
        }

        public static SplitterStep SplitThrough(this SingleThrowStep step, StepInstruction splitted, List<StepInstruction> instructions, PromptCommandRequest request)
        {
            var split = step.SplitTo(splitted, instructions, request);

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
        /// <param name="request"></param>
        /// <returns></returns>
        public static SingleThrowStep Feed(this SingleThrowStep step, StepInstruction instruction, List<StepInstruction> instructions, PromptCommandRequest request)
        {
            var split = step.Plug(instructions, request);

            step.Link(split, isForward: true, isTwoWay: true);

            var feeded = split.ExpandTo<JunctionStep>(instruction.Command, request, instruction.FeedFwdInstruction);

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
        public static PipedStep Pipe(this SplitterStep step, PromptCommandRequest request, IJsoneable command, string? pipeFeedFwd = null)
        {
            var split = step.ExpandTo<PipedStep>(new StepInstruction(command, pipeFeedFwd), request);

            step.Link(split, isForward: true, isTwoWay: true);

            return split;
        }
        
        public static SingleThrowStep Join( this SplitterStep step, StepInstruction instruction, PromptCommandRequest request)
        {
            var next = step.ExpandTo<JunctionStep>(instruction.Command, request, instruction.FeedFwdInstruction);
            
            step.Link(next, isForward: true, isTwoWay: true);
            
            return next;
        }

        // instructions for CURRENT (c):            c = Extract User Intent
        //  stepA = what to do with c IN a          c-a = Explain in less then 50
        //  stepB = what to do with c IN b          c-b = Add related topics of interest
        //  fwd = what to do with a IN b        a-b = [necesito por cojones un tercer comando] stepA.ExpandTo(junCmd) || [si es seguro] stepA.ExpandTo<SomeSpecificCmd, SurelyAMergeCmdInTextOpt> (merge, enhance, reduce) 
        //                                                                                                                || .And<SomeJunctionCommandIWant>(cmdA, cmdB, msgs dict?)

        //                                                                                                                   StartWith(userIntent)
        //                                                                                                                      .And<MergeResultsCommand>(cmdA, cmdB)
        //                                                                                                                      .Then(cmdC)

        //                                                                                                                  StartWith(selectCharacterName)
        //                                                                                                                      .Feed<CreateFullProfileCommand>(
        //                                                                                                                          new StepInstruction(selectFactionAndGameStuffForNameCMD, feedFwdA: "Feed for CreateFullProfileCmd")
        //                                                                                                                          new StepInstruction(createBackgroundStoryForNameCMD), feedFwdB: "Feed for CreateFullProfileCmd")
        //                                                                                                                          feedFwdMessg: "Use the generated profile to create image, review game stuff too blah blah
        //                                                                                                                      .Then(createNFTImageFromIconicMoment)
        //                                                                                                                      .Then(storeInChroma | storeInMongo)
        //                                                                                                                      .ThenExecuteAsync(request)
        //  final = what to do with b IN next       return b
        public static SingleThrowStep And<TJunctionComm, TJunctionRes>(this SingleThrowStep step, PromptCommandRequest request, IJsoneable cmdA, IJsoneable cmdB, string junctionInstruction, 
            string? finalFwdInstruction = null, string? fwdInstructionA = null, string? fwdInstructionB = null, string? junctionFwd= null)
            where TJunctionComm : BasePromptCommand<TJunctionRes>, new()
        {
           
            //stepA.Link(junction, isForward: true, isTwoWay: true);                                       ------B`4 ---
            //                                                                                            |  ,-- B`2 ---|
            //                                                                                           FEED --- B`3 - THEN --(b)--           
            //                                                                                            |              |         |
             //                                                                                  |- B  -- B` --- B" -----|         |
            //                                                                                A  -  C  -- C` --- C" ---- D --(a)-- E -- OPT 
            //junction.Link(stepB, )                                                       START   TAP  PIPE   PIPE    JOIN   THEN
            // STEPS = SWITCHES = SPST | SPDT | DPST | DPTD (two-way pipe / pipe bridge --> SPST - SPDT - DPDT- DPDT- DPST - SPST)
            //step.Split(
            //  out var stepA.Then(A'),
            //  out var stepB.Then(B')
            //               .Then(B"),
            //)
            //.Join(A, B) esto es un marron

            //stepA.Link(stepB: isForward: true, isTwoWay: true
            //clone = step.Clone()
            //step.Link(stepA, isForward: true, isTwoWay: false);
            //clone.Link(stepB, isForward: true, isTwoWay: false);
            // var juntion = stepA.ExpandTo(cmd, request, juntionFwdInstruction);
            return step;
        }

        // And(A, B) --> opt = input * A + input * B
        //
        // Sequence (N, returnMaxTokens: 1000) --> opt = input * N & collect? (for N -> opt = N * input) equivalente a loop esto es .For() con el collect incorporado
        //  > usado con limite puede usarse para algo asi como MapAndReduce <- se añade SIEMPRE un eslabón de sumarizacion con MaxToken = param. Util para controlar CTX_LEN
        //
        // Switch (N, evaluator) --> opt = input * N + best scored eval 
        // 
        // .WithRelay(prompteable, voltageCondition) <- se activa si true, si no bypass
        // .AddCapacitor(1k tokens) <- si el sysmessage llega a valor de carga lo mantiene (con sumarizaciones o algo asi)
        // .Choke<TVal>(3 validations) where TVal : JsonValidatorCommand  <-- Request.CommandValidations + ChokeVals?

        //  [SPST con secuencia Input * Command + append] (.Pipe AKA .For abre la divide la cadena y la deja abierta (de SplitterStep a SplitterStep -> N a N | JunctionStep = 2 a 1 ----- 1 a N y 1 a 2, de N a N y de 2 a 2 (es redundante, solo hace falta 1 y N)
        //                                                                                                                                              Son 3 piezas y una con reversed: 1-1 , 1-N + (rev) y N-N
        //                                                                                                                                                          (multi-in, multi-out)
        // .Plug([cmds], capacity: 4k)
        //      if(cmds.Sysmsg.Sum() > capacity)
        //          step.ExpandTo<SummarizerCommand, ChatMessage>("create a single instruction from all this instructions, capture the core of each one and summarize a single clear instruction containing the core of each one")
        //      else FAN IN = input * command for command in plug ej: input = chat chunk, plug=[analyzeMood, extractLore, analyzeContext, determineInnterBotState], output= appendResults (texto con secciones para el siguiente, SelectOutputAction, por ejemplo)
        //      
        //      StartWith(GetLastChatChunkCommand)
        //      .Plug([
        //          _factory.getAsPrompteable<A>(),
        //          _factory.getAsPrompteable<B>(),
        //          _factory.getAsPrompteable<C>()
        //      ])
        //     .WireTo(_factory.getAsPrompteable<SelectActionCommand>()
        //     .ThenExecuteAsync()
        public static async Task<ChainResult> ThenExecuteAsync(this SingleThrowStep step, bool withFinalMessage = true)
            => await step.ExecuteChainAsync(withFinalMessage);
       
        public static ChainStep Then<TCommand, TResult>(this ChainStep step, string? instruction, PromptCommandRequest request, string? feedFwdInstruction = null) 
            where TCommand : BasePromptCommand<TResult>, new() 
            where TResult : class
        {
            var nextStep = step.ExpandTo<TCommand, TResult>(instruction, request);

            step.Link(nextStep, isForward: true, isTwoWay: true);

            return nextStep as ChainStep;
        }

        //public static ChainStep Then<TCommand, TResult>(this ChainStep step, PromptCommandRequest request,  PromptCommandRequest  commandRequest)
        //{
        //    //step.From<TCommand, typeof(PromptCommandRequest), 

        //    return step;
        //}
        public static TypedOutput<TResult> Then<TResult>(this TypedOutput<TResult> step, IPrompteable<TResult> cmd /*IPromptCommand<TResult> cmd*/, PromptCommandRequest request) where TResult : class
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
