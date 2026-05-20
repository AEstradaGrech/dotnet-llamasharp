namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class ForgeLog
    {
        public ForgeLog() 
        {
            ForgeTimestamp = null;
            RunnersLog = new List<string>();
        }
        public ForgeLog(Guid id, string? feedFwd = null) : this()
        {
            RunnerId = id;
            FeedForwardMessage = feedFwd;
        }
        public Guid RunnerId { get; set; }
        public Guid PrevId { get; set; }
        public Guid NextId { get; set; }
        public DateTime? ForgeTimestamp { get; set; }
        
        public string Prompt { get; set; }
        public string JsonResult { get; set; }
        public string JsonSchema { get; set; }
        public string CommandInstruction { get; set; }
        public string StepInstruction { get; set; } // tap (exception) -> append Branches.First().Instruction (no hay una instruccion prioritaria) >> Split es splitted command SIN subinstrucciones (goal = splitted + feedFwd(s)
        public string FeedForwardMessage { get; set; } //_feedFwd + subchains.FeedForward

        public List<string> RunnersLog { get; set; }
    }
}
