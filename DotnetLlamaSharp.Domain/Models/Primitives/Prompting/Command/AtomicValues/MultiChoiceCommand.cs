using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues
{
    public class MultiChoiceCommand : DbPromptCommand<List<string>>
    {
        public MultiChoiceCommand() { }
        public MultiChoiceCommand(IOllamaInferenceService ollama) : base(ollama) { }
        public MultiChoiceCommand(IOllamaInferenceService ollama, IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) : base(ollama, repo, dbMessageName, guidanceMessage, settings) { }

        public override async Task<List<string>> Prompt(PromptCommandRequest request)
        {
            validateInputRequest<MultiChoiceRequest>(request);

            var multiChoiceReq = (MultiChoiceRequest) request;

            if (multiChoiceReq.MaxSelections <= 0)
                multiChoiceReq.MaxSelections = 1;

            if (!multiChoiceReq.Choices.Any())
                throw new InvalidDataException($"{nameof(MultiChoiceCommand)} >> The requested number of selected choices is greater or equal to the available choices");

            var promptReq = await getGenerateRequest(multiChoiceReq);

            foreach (var choice in multiChoiceReq.Choices)
                promptReq.System += $"\n- {choice}";

            promptReq.System = promptReq.System.Replace("<<MAX_SEL>>", $"{multiChoiceReq.MaxSelections}");

            var response = await _ollama.CommandPrompt<MultiChoiceResponse>(promptReq, _settings.CommandValidations, _settings.ValidationType, validatorFor<MultiChoiceResponse>());

            return response.Selected;
        }

        protected override string getDefaultInstruction()
            => "Analyze the provided list of choices and select up to (but not necessarily) <<MAX_SEL>> options that matches the best with the user request, or an empty list if the user intent is unrelated to any available choice. Output a list of strings containing your selected values (if any) according to the provided JSON schema.\n> CHOICES:";
    }
}
