using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
   //
   // Executes all branch commands using the output of the previous link
   // generates 1 link x branch
   // if you concat a .Then after a .Spli,t IT APPENDS EVERY PREV OUTPUT AND RETURS ONE LINK. .Pipe(this Splitter) executes THE COMMAND ON EACH PREV OUTPUT AND GENERATES 1 X PREV OUT LINKS
   //   .Join<TJoiner>() merges EVERY PREV OUT LINK USING A SPECIFIC COMMAND (as opposed to the .Then 'raw' appending)
   // OPCION B: SingleThrowStep & SplitterStep extensions --> .Then para SingleThrow, pipe para Splitter, asi queda mas cerrado el uso (hasta que no haces Join no puedes usar .Then
   //           y el merge a las bravas del .Split().Then() pasaria a ser un .Join sin TJoiner. Es decir, tendria .Join(nextCommand) (append burro = ejecuto next con todo lo que haya prev) y
   //                                                                                                             .Join<TJoiner>(nextCommand) que interiormente sequencia un reducer con el nextCommand)
   //                                                                                                                es decir, procesa el multioutput como quiere el usuario y ese es el feed de nextCommand
   // Split([]) (NO A B)

    public class SplitterStep : ChainStep
    {
        protected List<IChaineable> _branches = new List<IChaineable>();
        protected Dictionary<IChaineable, Guid> _forgedSubSteps;

        public SplitterStep() : base() { }
        // Constructor for Tap (execute N commands with the PREV OUT) (foreach branch, forge(branch))
        public SplitterStep(List<StepInstruction> instructions, PromptCommandRequest request, string? feedFwdMessage) : base(request, feedFwdMessage)
        {
            _forgedSubSteps = new Dictionary<IChaineable, Guid>();
            instructions.ForEach(i => _branches.Add(ExpandTo(i.Command, request, i.FeedFwdInstruction)));
        }

        //Constructor fro Split<TComm>([]) <- executes the command and SPLITS the result over the plugged subchains passing the splitFeedFwd (forge(this) then Branches)
       
        public SplitterStep(StepInstruction splittedCommand, List<StepInstruction> instructions, PromptCommandRequest request) : base(request, splittedCommand.FeedFwdInstruction)
        {
            _forgedSubSteps = new Dictionary<IChaineable, Guid>();
            _commands.Add(splittedCommand.Command);
            instructions.ForEach(i => _branches.Add(ExpandTo(i.Command, request, i.FeedFwdInstruction)));
        }

        // constructor for .Pipe<TComm> <- executes the command ON EACH input (foreach prevOUT forge(this))
        public SplitterStep(StepInstruction pipedCommand, PromptCommandRequest request) : this(pipedCommand, [], request) { }
        // It is not the first step of a chain (prev != null)
        // The previous is a SPST
        // It has at leas one command to run (should assert it has at least two probably otherwise is a bad / stupid configuration)
        public override bool CanBeForged(IChaineable previous)
            => previous != null && !previous.IsMultiSocket && _branches.Count > 0;

        public void Plug(IChaineable branch)
        {
            _branches.Add(branch);
        }

        public override async Task<IChaineable> Forge(IChaineable previous) //En el momento que hago split tipado hace que solo pueda recibir SplitterStep hasta .Join()
        {
            checkCanForge(previous);

            if (previous.IsMultiSocket)
                throw new InvalidOperationException($"{nameof(SplitterStep)} >> BAD CHAIN CONFIGURATION >> PREVIOUS STEP IS MULTISOCKET >> A SplitterStep can only be connected from a SingleThrowStep");


            if (_next == null)
                throw new InvalidOperationException($"{nameof(SplitterStep)} >> BAD CHAIN CONFIGURATION >> A CHAIN cannot end up in a SplitterStep and the current has no NEXT assigned >> CHAIN CANNOTE BE CLOSED");

            if (!string.IsNullOrEmpty(_feedForwardInstruction))
                _instructionsLog.Add($"- SPLITTER: {_id}");

            
            if(_commands.Count == 1)
            {
                //is Split and FanOut

                var splittedOut = await ExpandTo(_commands.First(), _request, previous.FeedForwardInstruction).Forge(previous);

                previous = splittedOut;
            }

            var chainResults = new List<IChaineable>();

            processBranchResults(await Task.WhenAll(_branches.Select(branch => Task.Run(() => branch.Forge(previous)))));

            return await _next.Forge(this);
        }

        public IChaineable GetPluggedById(Guid stepId)
        {
            if (!_branches.Any(step => step.Id == stepId))
            {
                return _forgedSubSteps.Keys.Any(key => key.Id == stepId) ?
                    _branches.SingleOrDefault(branch => branch.Id == _forgedSubSteps.Select(kvp => kvp).Where(kvp => kvp.Key.Id == stepId).Select(x => x.Value).First()) :
                    null;
            }

            else return _branches.SingleOrDefault(step => step.Id == stepId);
        }

        public void processBranchResults(IChaineable[] results)
        {
            results.ToList().ForEach(chaineable =>
            {
                var first = chaineable.GetFirstStep();

                _forgedSubSteps.Add(chaineable, first.Id);

                Outputs.AddRange(chaineable.Outputs);

                _instructionsLog.Add($"- SUB CHAIN {chaineable.Id} LOG:");
                _instructionsLog.AddRange(chaineable.InstructionsLog);
            });
        }

        public IChaineable GetForgedSubStep(Guid stepId)
            => _forgedSubSteps.Keys.FirstOrDefault(step => step.Id == stepId);
    }
}
