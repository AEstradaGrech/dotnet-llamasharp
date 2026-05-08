using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues
{
    public class NumericPromptCommand : ChromaPromptCommand<float?>
    {
        public NumericPromptCommand() {}
        public NumericPromptCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null) : base(repo, dbMessageName, guidanceMessage) {}

        public override async Task<float?> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            var response = await ollama.StructuredPrompt<NumericResponse>(request.Prompt, await getPromptInstruction(), request.Model);

            return response.Result;
        }
    }
}
