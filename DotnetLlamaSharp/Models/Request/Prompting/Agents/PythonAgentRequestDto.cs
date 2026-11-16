using DotnetLlamaSharp.Models.Common;

namespace DotnetLlamaSharp.Models.Request.Prompting.Agents
{
    public class PythonAgentRequestDto : SimplePromptRequestDto
    {
        public PromptSettingsDto? ToolSettings { get; set; }
    }
}
