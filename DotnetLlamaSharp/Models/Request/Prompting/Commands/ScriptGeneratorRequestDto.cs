using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Enums;
using DotnetLlamaSharp.Models.Request.Prompting;

namespace DotnetLlamaSharp.Models.Request.Prompting.Commands
{
    public class ScriptGeneratorRequestDto : SimplePromptRequestDto
    {
        public string? RequestedShell { get; set; }
    }
}
