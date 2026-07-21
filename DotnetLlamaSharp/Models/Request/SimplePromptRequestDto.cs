using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Enums;
using DotnetLlamaSharp.Models.Common;

namespace DotnetLlamaSharp.Models.Request
{
    public class SimplePromptRequestDto
    {
        public string Prompt { get; set; }
        public string? SystemMessage { get; set; } = null;
        public bool IsGuidanceAppend { get; set; }
        public EReasoning? Reasoning { get; set; }
        public PromptSettingsDto Settings { get; set; }
    }
}
