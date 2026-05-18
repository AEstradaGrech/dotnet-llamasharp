namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class ReplayLog : ForgeLog
    {
        public ReplayLog() {}

        public ReplayLog(ForgeLog forgeLog)
        {
            RunnerId = forgeLog.RunnerId;
            PrevId = forgeLog.PrevId;
            NextId = forgeLog.NextId;
            CommandInstruction = forgeLog.CommandInstruction;
            FeedForwardMessage = forgeLog.FeedForwardMessage;
            Prompt = forgeLog.Prompt;
            ForgeTimestamp = forgeLog.ForgeTimestamp;
            JsonResult = forgeLog.JsonResult;
        }
        public DateTime PassCatchTimestamp { get; set; }

        public string FeededMessage { get; set; }

        public Dictionary<Guid, ReplayLog> BranchLogs { get; set; }
    }
}
