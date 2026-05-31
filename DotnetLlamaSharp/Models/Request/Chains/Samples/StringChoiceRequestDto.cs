namespace DotnetLlamaSharp.Models.Request.Chains.Samples
{
    public class StringChoiceRequestDto : SimplePromptRequestDto
    {
        public List<string> Choices { get; set; }
    }
}
