using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues
{
    public class StringChoiceCommand : ChromaPromptCommand<string>
    {
        public StringChoiceCommand() { }
        public StringChoiceCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) 
            : base(repo, dbMessageName, guidanceMessage, settings) {}

        public override async Task<string> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            var message = await getPromptInstruction();

            validateInputRequest<StringChoiceRequest>(request);

            foreach (var choice in ((StringChoiceRequest)request).Choices)
                message += $"\n- {choice}";

            var response = await ollama.StructuredPrompt<StringChoiceResponse>(request.Prompt, message, request.Model);

            return response.Selected;
        }

        protected override string getDefaultInstruction()
            => "Analyze the provided list of choices and select the value that matches the best with the user request. Output your selected choice according to the provided JSON schema.\n> CHOICES:";
    }
}
