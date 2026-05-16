
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public interface IChaineable
    {
        bool IsChained(bool? checkNextOnly = true);
        bool IsFirstStep();
        
        public void Link(IChaineable next, bool isForward, bool isTwoWay = false);
        Task<IChaineable> Forge(IChaineable previous);
        public Task<ChainResult> ExecuteChain(bool withUserFriendlyMessage = true);
        IChaineable ExpandTo(IJsoneable command, PromptCommandRequest request, string? feedForwardInstruction = null);
        IChaineable ExpandTo<TCommand, TResult>(string instruction, PromptCommandRequest request, string? feedForwardInstruction = null) where TCommand : BasePromptCommand<TResult>, new();
        TDeserialized GetOutputAs<TDeserialized>() where TDeserialized : class;
        public IChaineable Previous { get; }
        public IChaineable Next { get; }
        public IJsoneable Command { get; }
        public List<string> InstructionsLog { get; }
        public string? PromptedInstruction { get; }
        public string Input { get; }
        public bool IsPreloaded { get; }
        public ChainLink OutputLink { get; }
        
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
