using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Enums;

namespace DotnetLlamaSharp.Models.Request
{
    public class ScriptGeneratorRequestDto : SimplePromptRequestDto
    {
        public string? RequestedShell { get; set; }
    }
}
