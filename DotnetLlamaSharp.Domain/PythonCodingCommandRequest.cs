

using Dotnet.OllamaSharp.LameChain.SDK.Commands.Response.StructuredOutputs;
using DotnetLlamaSharp.Domain.Models.Request;

namespace DotnetLlamaSharp.Domain
{
    public class PythonCodingCommandRequest : SimpleCommandRequest
    {
        public required PythonGeneratorResponse CodeRequest { get; set; }
    }
}
