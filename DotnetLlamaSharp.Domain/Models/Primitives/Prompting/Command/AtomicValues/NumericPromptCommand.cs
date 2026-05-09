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
        public NumericPromptCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) : base(repo, dbMessageName, guidanceMessage, settings) {}

        public override async Task<float?> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            var promptRequest = await getGenerateRequest(request); //pa todos
            //var response = await ollama.StructuredPrompt<NumericResponse>(getGenerateRequest(requestSettings()), validations: _validations);
            var response = await ollama.StructuredPrompt<NumericResponse>(await getGenerateRequest(request), _settings.CommandValidations);

            return response.Result;
        }
    }
}
