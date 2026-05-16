using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues
{
    public class StringChoiceCommand : DbPromptCommand<string>
    {
        public StringChoiceCommand() : base() { }
        public StringChoiceCommand(IOllamaInferenceService ollama) : base(ollama) { }
        public StringChoiceCommand(IOllamaInferenceService ollama, IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) 
            : base(ollama, repo, dbMessageName, guidanceMessage, settings) {}

        public override async Task<string> Prompt(PromptCommandRequest request)
        {
            var promptReq = await getGenerateRequest(request);

            validateInputRequest<StringChoiceRequest>(request);

            foreach (var choice in ((StringChoiceRequest)request).Choices)
                promptReq.System += $"\n- {choice}";

            var response = await _ollama.CommandPrompt<StringChoiceResponse>(promptReq, _settings.CommandValidations, _settings.ValidationType, validatorFor<StringChoiceResponse>());

            return response.Selected;
        }

        protected override string getDefaultInstruction()
            => "Analyze the provided list of choices and select the value that matches the best with the user request. Output your selected choice according to the provided JSON schema.\n> CHOICES:";
    }
}
