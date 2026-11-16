
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Enums;

namespace DotnetLlamaSharp.Domain.Models.Request.Prompting
{
    public class ScriptGeneratorRequest : SimpleCommandRequest
    {
        public EScriptShell? RequestedShell { get; set; }
    }
}
