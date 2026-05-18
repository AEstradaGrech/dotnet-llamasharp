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
        }

        public ChainRunner(ChainRunner cloned) : this(cloned.UserPrompt, cloned.Name)
        {
            RunnedInstructions = cloned.RunnedInstructions;
            StepInstructions = cloned.StepInstructions;
        }

        public delegate void OnReplay();
        public event OnReplay onReplayRequest;

        public List<ForgeLog> _forgedSteps;
        public List<ReplayLog> _replays;

        public void OnRunnerNotification(ForgeLog log)
        {
            _forgedSteps.Add(log);
        }

        public void OnReplayReport(ReplayLog log)
        {
            _replays.Add(log);
        }

        public string Name { get; }
        public string UserPrompt { get;}
        // instructions by step forge
        public Dictionary<Guid, List<string>> StepInstructions { get; }
        // instructions executed so far. accumulated from all previous steps
        public List<string> RunnedInstructions { get; }

        public ChainRunner Clone() => new ChainRunner(this);
    }
}
