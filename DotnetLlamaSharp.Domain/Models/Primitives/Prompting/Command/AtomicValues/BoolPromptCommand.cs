using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues
{
    public class BoolPromptCommand : ChromaPromptCommand<bool>
    {
        public BoolPromptCommand() :base() { }
        //Todo esto se pasa desde la factory, que si tiene DI
        public BoolPromptCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null) : base(repo, dbMessageName, guidanceMessage)
        {
        }

        public override async Task<bool> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            var response = await ollama.StructuredPrompt<BooleanResponse>(request.Prompt, await getPromptInstruction(), request.Model);

            return response.Answer;
        }
    }
}
