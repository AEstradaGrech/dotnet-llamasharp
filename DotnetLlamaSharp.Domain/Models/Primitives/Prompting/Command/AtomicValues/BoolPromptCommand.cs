using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues
{
    public class BoolPromptCommand : DbPromptCommand<bool>
    {
        public BoolPromptCommand() : base() { }
        public BoolPromptCommand(IOllamaInferenceService ollama) : base(ollama) { }
        // Values from factory injected services
        public BoolPromptCommand(IOllamaInferenceService ollama, IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) 
            : base(ollama, repo, dbMessageName, guidanceMessage, settings) { }

        public override async Task<bool> Prompt(PromptCommandRequest request)
        {
            var response = await _ollama.CommandPrompt<BooleanResponse>(await getGenerateRequest(request), _settings.CommandValidations, _settings.ValidationType, validatorFor<BooleanResponse>());

            return response.Answer;
        }

        protected override string getDefaultInstruction()
            => "Analyze the user question, reason your response and output a boolean to indicate YES / NO according to the provided JSON schema.";
    }
}
