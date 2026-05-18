using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    // Nested in ChainResult
    // Captures a detailed log of what happened during the chain
    // in terms of system message / step feed
    // This is for debugging purposes mostly so it is an option:
    //      - ThenExecuteAsync(withFinalMessage: y/n, withReport: y/n)
    //  chain.OnReport += step.ReportStatus()
    //  step.OnReported += chain.OnReportReceived(report)
    public class ChainRunner
    {
        public ChainRunner(string userInput, string? name = null)
        {
            UserPrompt = userInput;
            Name = name ?? $"{Guid.NewGuid()}-{DateTime.Now.ToShortDateString()}";
            _forgedSteps = new List<ForgeLog>();
            _replays = new List<ReplayLog>();

            RunnedInstructions = new List<string>();
        }

        public ChainRunner(ChainRunner cloned) : this(cloned.UserPrompt, cloned.Name)
        {
            RunnedInstructions = new List<string>();
        }

        public delegate void OnReplay();
        public event OnReplay onReplayRequest;

        public List<ForgeLog> _forgedSteps;
        public List<ReplayLog> _replays;

        public string Name { get; }
        public string UserPrompt { get; }
        public List<ForgeLog> ForgedLogs => _forgedSteps;
        private List<ReplayLog> ReplayLogs => _replays;
        public List<string> RunnedInstructions { get; set; }

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

        public ForgeLog GetLogById(Guid id) => _forgedSteps.SingleOrDefault(step => step.RunnerId == id);
    }
}
