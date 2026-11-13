using DotnetLlamaSharp.Models.Response;

namespace DotnetLlamaSharp.Models.Request.Chains.Samples
{
    public class PythonExecutorRequestDto : SimplePromptRequestDto
    {
        public required PythonGeneratorResponseDto CodeRequest { get; set; }
    }
}
