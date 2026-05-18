using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class SplitterStep : ChainStep
    {
        protected List<StepInstruction> _pluggedInstructions;

        protected List<IChaineable> _branches = new List<IChaineable>();
        protected Dictionary<IChaineable, Guid> _forgedSubSteps;

        public SplitterStep() : base() { }
        // Constructor for Tap (execute N commands with the PREV OUT) (foreach branch, forge(branch))
        public SplitterStep(List<StepInstruction> instructions, PromptCommandRequest request, string? feedFwdMessage) : base(request, feedFwdMessage)
        {
            _forgedSubSteps = new Dictionary<IChaineable, Guid>();
            _pluggedInstructions = instructions;
        }

        // Constructor for Split<TComm>([]) <- executes the command and SPLITS the result over the plugged subchains passing the splitFeedFwd (forge(this) then Branches)
       
        public SplitterStep(StepInstruction splittedCommand, List<StepInstruction> instructions, PromptCommandRequest request) : base(request, splittedCommand.FeedFwdInstruction)
        {
            _forgedSubSteps = new Dictionary<IChaineable, Guid>();
            _commands.Add(splittedCommand.Command);
            _pluggedInstructions = instructions;
        }

        // Constructor for .Pipe<TComm> <- executes the command ON EACH input (foreach PREV OUT forge(this))
        public SplitterStep(StepInstruction pipedCommand, PromptCommandRequest request) : this(pipedCommand, [], request) { }
        
        // It is not the first step of a chain (prev != null)
        // The previous is a SPST
        // It has at least one command to run (should assert it has at least two probably otherwise is a bad / stupid configuration)
        public override bool CanBeForged(IChaineable previous)
            => IsRunning && previous != null && !previous.IsMultiSocket && _pluggedInstructions.Count > 0;

        public void Plug(IChaineable branch)
        {
            _branches.Add(branch);
        }

        public override async Task<IChaineable> Forge(IChaineable previous)
        {
            if (!hasCatchedPass(previous))
                throw new InvalidOperationException($"{nameof(SplitterStep)} >> {nameof(Forge)} >> {nameof(hasCatchedPass)} >> An error has occured while passing the runner. STEP CANNOT BE FORGED");

            if (previous.IsMultiSocket)
                throw new InvalidOperationException($"{nameof(SplitterStep)} >> BAD CHAIN CONFIGURATION >> PREVIOUS STEP IS MULTISOCKET >> A SplitterStep can only be connected from a SingleThrowStep");

            if (_next == null)
                throw new InvalidOperationException($"{nameof(SplitterStep)} >> BAD CHAIN CONFIGURATION >> A CHAIN cannot end up in a SplitterStep and the current has no NEXT assigned >> CHAIN CANNOT BE CLOSED");

            if (!string.IsNullOrEmpty(_feedForwardInstruction))
                _instructionsLog.Add($"- SPLITTER: {_id}");

            if(_commands.Count == 1)
            {
                var splittedOut = await PassTo(swapRunner: false, _commands.First(), _request, previous.FeedForwardInstruction).Forge(previous);

                _runner = splittedOut.PassRunner();

                previous = splittedOut;
            }

            _pluggedInstructions.ForEach(i => _branches.Add(PassTo(swapRunner: true, i.Command, _request, i.FeedFwdInstruction)));

            var chainResults = new List<IChaineable>();

            processBranchResults(await Task.WhenAll(_branches.Select(branch => Task.Run(() => branch.Forge(previous)))));

            submitForgeLog();

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
