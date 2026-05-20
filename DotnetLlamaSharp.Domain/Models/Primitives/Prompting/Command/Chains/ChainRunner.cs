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

        public ChainRunner(ChainRunner cloned) : this(cloned.UserPrompt, cloned.DefaultSettings, cloned.FwdSystemMessage,cloned.Intent)
        {
            RunnedInstructions = new List<string>();
        }
        public delegate void OnSupporterRequest(Guid runnedId);
        public event OnSupporterRequest onSupportRequest; // v2 -> Reviewer --> comprueba estos outputs vs sus inputs, evalua si el resultado es lo que le habian pedido, si no -> Re-run = onSupportRequest(id) triggers -> [spporter] onSupport(this) = _runner.OnSupportReceived(IChaineable) -> ThrowTo(supporter)

        public delegate void OnReplay();
        public event OnReplay onReplayRequest;

        public List<ForgeLog> _forgedSteps;
        public List<ReplayLog> _replays;

        public void OnSupporterResponse(IChaineable supporter)
        {
            // Runner.ThrowTo(supporter)
            // await supporter.Forge(Runner | Runner.Prev)
            // collect results -> feed to runner -> keep running
            
        }

        public void OnDropTo(IChaineable runner, IChaineable previous)
        {
            Runner = runner;

            onSupportRequest += runner.OnRunnerSupport;
            onReplayRequest += runner.SendReplay;

            Supporters.Add(previous.Id, previous.OnRunnerCall);
        }

        public string Intent { get; }
        public string UserPrompt { get; }
        public string? FwdSystemMessage { get; }
        public List<ForgeLog> ForgedLogs => _forgedSteps;
        private List<ReplayLog> ReplayLogs => _replays;
        public List<string> RunnedInstructions { get; set; }
        public CommandSettings DefaultSettings { get; set; }
        public IChaineable Runner { get; set; }
        public Dictionary<Guid, Func<IChaineable>> Supporters { get; set; }

        public void OnRunnerNotification(ForgeLog log, bool updateRunnersLog = true)
        {
            _forgedSteps.Add(log);

            if (updateRunnersLog)
                RunnedInstructions.AddRange(log.RunnersLog);
        }


        public void OnReplayReport(ReplayLog log)
        {
            _replays.Add(log);
        }


        public ChainRunner Clone() => new ChainRunner(this);

        
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
