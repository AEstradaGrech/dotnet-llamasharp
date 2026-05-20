
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public interface IChaineable
    {
        Guid Id { get; }
        bool IsChained(bool? checkNextOnly = true);
        bool IsFirstStep();
        public bool CanBeForged(IChaineable previous);
        bool IsMultiSocket { get; }
        public bool IsForged { get; }
        public ChainRunner? Runner { get; }
        public bool IsRunning { get; }
        public void Link(IChaineable next, bool isForward, bool isTwoWay = false);
        Task<IChaineable> Forge(IChaineable previous);
        ChainRunner Drop(); // passes the runner to the step requesting it, and nulls the reference? (game rule: there can be only ONE runner / ONE ball in the match field. A step without runner !CanBeForged(previous))
        //                                                                                            La referencia NO es null si eres el portador del balon. EL balon está o en juego o en la zona de try (lo tiene el ultimo)
        bool IsReady();
        void SendReplay();
        IChaineable OnRunnerCall();
        void OnRunnerSupport(Guid id);
        void FollowRunner(IChaineable current);
        TStep ExpandTo<TStep>(IJsoneable command, ChainPromptRequest request, string? feedForwardInstruction = null) where TStep : ChainStep;
        SingleThrowStep ExpandTo(IJsoneable command, ChainPromptRequest request, string? feedForwardInstruction = null);
        SplitterStep Plug(List<StepInstruction> commands, ChainPromptRequest request, string? splitterFeedFwd = null);
        TStep ExpandTo<TStep, TCommand, TResult>(string instruction, ChainPromptRequest request, string? feedForwardInstruction = null) where TCommand : BasePromptCommand<TResult>, new() where TStep : ChainStep;
        SingleThrowStep ExpandTo<TCommand, TResult>(string instruction, ChainPromptRequest request, string? feedForwardInstruction = null) where TCommand : BasePromptCommand<TResult>, new();
        TDeserialized GetOutputAs<TDeserialized>() where TDeserialized : class;
        public IChaineable Previous { get; }
        public IChaineable Next { get; }
        public List<IJsoneable> Commands { get; }
        public List<string> InstructionsLog { get; }
        public string? PromptedInstruction { get; }
        public string? FeedForwardInstruction { get; }
        public string Input { get; }
        public List<ChainLink> Outputs { get; }
        public Dictionary<Guid, List<ChainLink>> GrouppedOutputs();
        public IChaineable GetFirstStep(); // returns the first step connected to caller step
        /*
            Extensions:

                _cmd1.Then(request, cmdName)  <- ejectuta y sigue
                     .And(request, cmdName2, cmdName3) <- ejecuta primero 2 y 3  luego sigue la cadena
                     .Then(request, cmdName4)
                     .For({
                         CmdA.Chain(cmdB),        <- 
                         CmdC,
                         CmdA.Chain(cmdC),
                         CmdB.Chain(cmdX).Then(cmdY).And(cmdZ)
                      })
                      .Collect(_summarizationCommand) <- las fuentes de sumarizacion se procesan diferente por las cadenas de comandos
                      .Branch(
                           request,
                           evaluator: CmdJ,
                           trueOption: CmdL.And(request, CmdM, CmdN),
                           falseOption: CmdE.Then(CmdF),
                           continueWithOption: true | false
                        )
                      .Then(...)

        OPCION B:

        
        var finalMessage = 
            await Step<TCommand, TResult>(request, new PromptCommandReq&Sons(x info para este command))
                .Then<TCommand2, TResult2>(request, new PromptCommandReq&Sons)  <- ejectuta y sigue
                .And(request, cmdName2, cmdName3) <- ejecuta primero 2 y 3  luego sigue la cadena
                .Then(request, cmdName4)
                .For({
                    Step<TCommand2>(request).Then<TCommandB>(request, ),        <- 
                    Step<TCommad3>(request),
                    Step<TComand4>(request).Then<TCommandC>(request),
                    CmdB.Chain(cmdX).Then(cmdY).And(cmdZ)
                })
                .Collect(_summarizationCommand) <- las fuentes de sumarizacion se procesan diferente por las cadenas de comandos
                .Branch(
                    request,
                    evaluator: CmdJ,
                    trueOption: CmdL.And(request, CmdM, CmdN),
                    falseOption: CmdE.Then(CmdF),
                    continueWithOption: true | false
                )
                .Then(...)
                .EndChain() // devuelve Message, no Step
                

            
            .Then(IJsoneable cmd)
            .Then<TCommand, TResult>(request)
            .Sequence(request, [commandNames]) <- No es de T porque T es para instanciar 1 command
            .And<T1, T2>(request
            .And(request, 
                Step<T1>(request)       
                    .Then<T3>(request), 
                Step<T2>
            )  --> ((T1+T3) + T2) + ...
               --> ((analyze + summarize | update KG) + evaluateResult | evaluateMood) + ...

         */
    }
}
