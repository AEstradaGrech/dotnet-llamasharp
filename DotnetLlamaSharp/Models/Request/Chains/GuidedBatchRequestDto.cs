namespace DotnetLlamaSharp.Models.Request.Chains
{
    public class GuidedBatchRequestDto : SimplePromptRequestDto
    {
        public List<string> Instructions { get; set; }
    }
}
