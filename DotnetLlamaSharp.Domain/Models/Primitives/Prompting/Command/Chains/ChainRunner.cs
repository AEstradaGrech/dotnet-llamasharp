namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
   
    public class ChainRunner
    {
        //          'Generate Character in this specific way',  'Finally do X',     'Character Creation' (overall goal)
        public ChainRunner(string userInput, CommandSettings defaultSettings, string? finalSysMessage = null, string? intent /*AKA topic?*/= null)
        {
            UserPrompt = userInput.Trim();
            Intent = string.IsNullOrEmpty(intent.Trim()) ? null : intent;
            FwdSystemMessage = finalSysMessage;
            DefaultSettings = defaultSettings;

            Supporters = new Dictionary<Guid, Func<IChaineable>>();
            RunnedInstructions = new List<string>();
            _forgedSteps = new List<ForgeLog>();
            _replays = new List<ReplayLog>();
        }

        public ChainRunner(ChainRunner cloned, bool cloneRunnedInstructions) : this(cloned.UserPrompt, cloned.DefaultSettings, cloned.FwdSystemMessage,cloned.Intent)
        {
            RunnedInstructions = cloneRunnedInstructions ? cloned.RunnedInstructions : new List<string>();
        }
        public delegate void OnSupporterRequest(Guid runnedId);
        public event OnSupporterRequest onSupportRequest; // v2 -> Reviewer --> comprueba estos outputs vs sus inputs, evalua si el resultado es lo que le habian pedido, si no -> Re-run = onSupportRequest(id) triggers -> [spporter] onSupport(this) = _runner.OnSupportReceived(IChaineable) -> ThrowTo(supporter)

        public delegate void OnReplay();
        public event OnReplay onReplayRequest;

        private IChaineable _current;
        public List<ForgeLog> _forgedSteps;
        public List<ReplayLog> _replays;

        public void OnSupporterResponse(IChaineable supporter)
        {
            // Runner.ThrowTo(supporter)
            // await supporter.Forge(Runner | Runner.Prev)
            // collect results -> feed to runner -> keep running
            
        }

        public void SetReady(IChaineable firstRunner)
        {
            if (_current != null) return;

            _current = firstRunner;

            onReplayRequest += _current.SendReplay;
        }

        // runner recievesThrow(previous)
        // _runner = previous.Drop()        <- pass the ChainRunner object to the next
        // _runner.OnDrop(this, previous)   <- sets runner as current and the previous as supporter (events & tracking)
        //      previous.FollowRunner()     <- subscribes to the support and replay (or any 'past-runner' events)
        // current.onRunnerNotify += OnRunnerNotification <- current can talk TO ChainRunner object
        // current.CatchThrowTimestamp = NOW; <- everything OK, start running
        // IsRunning = true                   <- has the _runner
        public void OnDropTo(IChaineable runner, IChaineable previous)
        {
            //This is the new runner holding the ChainRunner
            _current = runner;

            //If a previous runner (lost back in the chain) is required
            // the runner will trigger a multicast event with the id
            onSupportRequest += previous.OnRunnerSupport;
            // All previous are now supporters following the runner in case they are needed to
            //  - get more info about its outputs
            //  - replay its task (for validation-retry commands)
            // to do that they must respond to the support request
            // they register to a callback here and now the 'follow' the runner
            previous.FollowRunner(runner);
            
            // this might be triggered at the end of the chain to
            // get detailed info about the runned step
            onReplayRequest += previous.SendReplay; //WG: el último no se suscribe. CHECK

            // There is also a fast dictionary-delegate tracking all
            // the supporters (might be removed or set as the final access system better than the events system)
            Supporters.Add(previous.Id, previous.OnRunnerCall);
        }

        public string Intent { get; }
        public string UserPrompt { get; }
        public string? FwdSystemMessage { get; }
        public List<ForgeLog> ForgedLogs => _forgedSteps;
        
        public List<string> RunnedInstructions { get; private set; }
        public CommandSettings DefaultSettings { get; set; }
        public IChaineable Runner => _current;
        public Dictionary<Guid, Func<IChaineable>> Supporters { get; set; }

        public bool CanRun() => _current != null && !string.IsNullOrEmpty(UserPrompt) && DefaultSettings != null;

        public void OnRunnerNotification(ForgeLog log, bool updateRunnersLog = true)
        {
            _forgedSteps.Add(log);

            if (updateRunnersLog)
                RunnedInstructions.AddRange(log.RunnersLog);
        }
        public List<ReplayLog> GetReplays()
        {
            if (_replays.Count == 0)
                onReplayRequest();

            return _replays.Count == 0 ? [] : 
                _replays.OrderBy(replay => replay.PassCatchTimestamp).ToList();
        }

        public void OnReplayReport(ReplayLog log)
        {
            _replays.Add(log);
        }


        /// <summary>
        /// Use this to clone and swap the ChainRunner in FIRST SUBRUNNERS
        /// By default they get a copy of what has happened so far. 
        /// Also, it is a SDK rule: FIRST SUBRUNNERs are differenced from the main FIRST RUNNER
        /// because the total number of instructions runned in the chain
        /// </summary>
        /// <param name="withRunnedInstructions"></param>
        /// <returns></returns>
        public ChainRunner Clone(bool withRunnedInstructions = true) => new ChainRunner(this, withRunnedInstructions);

        
        public List<ChainLink> OutputsFromRunned(Guid id)
        {
            var results = new List<ChainLink>();

            if (tryRequestSupport(id, out var runned))
                results.AddRange(runned.Outputs);

            return results;
        }
        private bool tryRequestSupport(Guid id, out IChaineable supporter)
        {
            supporter = Supporters.ContainsKey(id) ? Supporters[id]() : null;

            //onSupportRequest(id);

            return supporter != null;
        }
        // IChaineable handleSupporter(reported)

        public bool TryFindForged(Guid id, out ForgeLog log)
        {
            log = null;

            if (_forgedSteps.Any(log => log.RunnerId == id))
                log = _forgedSteps.SingleOrDefault(log => log.RunnerId == id);
            
            return log != null;
        }
    }
}
