
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class ScoredChoiceCommand : DbPromptCommand<ScoredStringChoice>
    {
        public ScoredChoiceCommand() : base() { }
        public ScoredChoiceCommand(IOllamaInferenceService ollama) : base(ollama) { }
        public ScoredChoiceCommand(IOllamaInferenceService ollama, IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) 
            : base(ollama, repo, dbMessageName, guidanceMessage, settings) {}

        public override async Task<ScoredStringChoice> Prompt(PromptCommandRequest request)
        {
            validateInputRequest<StringChoiceRequest>(request);

            var promptReq = await getGenerateRequest(request);

            foreach (var choice in ((StringChoiceRequest)request).Choices)
                promptReq.System += $"\n- {choice}";

            return await _ollama.CommandPrompt<ScoredStringChoice>(promptReq, _settings.CommandValidations, _settings.ValidationType, validatorFor<ScoredStringChoice>());
        }

        protected override string getDefaultInstruction()
            => @"Analyze the provided list of choices and select the value that matches the best with the user request. 
Your task is not only to select the best choice but reason why to add a 'confidence score' to your response and a 'justification' comment of 15-20 words long explaining your choice selection and confidence score. 
Output your response according to the provided JSON schema.

> CHOICES:
";
    }
}
