using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class SimplePromptRequest : BasePromptRequest
    {
        public PromptSettings Settings { get; set; }
    }
}
