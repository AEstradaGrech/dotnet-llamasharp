namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class ForgeLog
    {
        public ForgeLog() { }
        public ForgeLog(Guid id, string? feedFwd = null)
        {
            RunnerId = id;
            FeedForwardMessage = feedFwd;
        }
        public Guid RunnerId { get; set; }
        public Guid PrevId { get; set; }
        public Guid NextId { get; set; }
        public DateTime ForgeTimestamp { get; set; }
        
        public string Prompt { get; set; }
        public string JsonResult { get; set; }
        public string CommandInstruction { get; set; } // tap (exception) -> append Branches.First().Instruction (no hay una instruccion prioritaria) >> Split es splitted command SIN subinstrucciones (goal = splitted + feedFwd(s)
        public string FeedForwardMessage { get; set; } //_feedFwd + subchains.FeedForward
    }
}
