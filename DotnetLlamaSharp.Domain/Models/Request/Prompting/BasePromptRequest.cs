using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Enums;

namespace DotnetLlamaSharp.Domain.Models.Request.Prompting
{
    /// <summary>
    /// App base prompt request for both the Command and No-Command requests
    /// </summary>
    public class BasePromptRequest
    {
        public string Prompt { get; set; }
        public string SystemMessage { get; set; } = string.Empty;
        public bool IsGuidanceAppend { get; set; }
        public EReasoning? Reasoning { get; set; }
    }
}
