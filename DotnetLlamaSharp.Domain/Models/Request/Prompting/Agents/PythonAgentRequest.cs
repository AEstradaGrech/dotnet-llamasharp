
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;
using DotnetLlamaSharp.Domain.Models.Request.Prompting.Commands;

namespace DotnetLlamaSharp.Domain.Models.Request.Prompting.Agents
{
    public class PythonAgentRequest : SimpleCommandRequest
    {
        public CommandSettings? ToolSettings { get; set; }
    }
}
