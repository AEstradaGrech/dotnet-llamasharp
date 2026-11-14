using DotnetLlamaSharp.Models.Common;

namespace DotnetLlamaSharp.Models.Request
{
    public class PythonAgentRequestDto : SimplePromptRequestDto
    {
        public PromptSettingsDto? ToolSettings { get; set; }
    }
}
