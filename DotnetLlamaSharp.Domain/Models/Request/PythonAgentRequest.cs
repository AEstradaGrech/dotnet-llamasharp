
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class PythonAgentRequest : SimpleCommandRequest
    {
        public CommandSettings? ToolSettings { get; set; }
    }
}
