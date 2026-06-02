using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;
using DotnetLlamaSharp.Domain.Models.Request.Prompting;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    /// <summary>
    /// Base request for al LameChain requests (with command settings)
    /// </summary>
    public class SimpleCommandRequest : BasePromptRequest
    {
        public CommandSettings Settings { get; set; }
    }
}
