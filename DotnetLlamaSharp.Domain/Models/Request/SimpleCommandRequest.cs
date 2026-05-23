using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class SimpleCommandRequest : BasePromptRequest
    {
        public CommandSettings Settings { get; set; }
    }
}
