using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class ScoredBoolCommand : ChromaPromptCommand<ScoredBoolResponse>
    {
        public ScoredBoolCommand() {}

        public ScoredBoolCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) 
            : base(repo, dbMessageName, guidanceMessage, settings) {}

        public override async Task<ScoredBoolResponse> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
            => await ollama.StructuredPrompt<ScoredBoolResponse>(request.Prompt, await getPromptInstruction(), request.Model);
    }
}
