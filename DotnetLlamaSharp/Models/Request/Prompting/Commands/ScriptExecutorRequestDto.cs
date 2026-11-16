using DotnetLlamaSharp.Models.Request.Prompting;

namespace DotnetLlamaSharp.Models.Request.Prompting.Commands
{
    public class ScriptExecutorRequestDto : SimplePromptRequestDto
    {
        public required string ScriptPath { get; set; }
    }
}
