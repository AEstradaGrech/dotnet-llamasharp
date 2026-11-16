using DotnetLlamaSharp.Models.Request.Prompting;
using DotnetLlamaSharp.Models.Response.Prompting.Commands;

namespace DotnetLlamaSharp.Models.Request.Chains.Samples
{
    public class PythonExecutorRequestDto : SimplePromptRequestDto
    {
        public required PythonGeneratorResponseDto CodeRequest { get; set; }
    }
}
