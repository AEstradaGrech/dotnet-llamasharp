using DotnetLlamaSharp.Models.Common;

namespace DotnetLlamaSharp.Models.Request.Chains
{
    public class ChainedPromptDto : SimplePromptRequestDto
    {
        public bool WithReport { get; set; }
        public bool WithFinalMessage { get; set; }
        public string? FinalSystemMessage { get; set; } = string.Empty;
        public PromptSettingsDto? FinalMessageSettings { get; set; } = null; // ?? Default (Input)
        public List<InstructionDto> Instructions { get; set; } = new List<InstructionDto>(); 
    }
}
