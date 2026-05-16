using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class ScoredBoolCommand : DbPromptCommand<ScoredBoolResponse>
    {
        public ScoredBoolCommand() : base() { }
        public ScoredBoolCommand(IOllamaInferenceService ollama) : base(ollama) { }

        public ScoredBoolCommand(IOllamaInferenceService ollama, IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) 
            : base(ollama, repo, dbMessageName, guidanceMessage, settings) { }

        public override async Task<ScoredBoolResponse> Prompt(PromptCommandRequest request)
            => await _ollama.CommandPrompt<ScoredBoolResponse>(
                _settings.UseDefaultCommandMessage ? 
                getGenerateRequest(request, withInstruction: false).Result : 
                await getGenerateRequest(request), _settings.CommandValidations, 
                _settings.ValidationType, 
                validatorFor<ScoredBoolResponse>());

        protected override string getDefaultInstruction()
            => @"Analyze the user request and reason a coherent response that can be sythetized in a boolean response to indicate 'YES' or 'NO' according to the provided JSON schema. 
Also, add a 'confidence' score to your response ranging from 0.0 to 1.0 to indicate how sure you are about your answer, and a 'justification' comment of 10-15 words long explaining your answer. Output your response according to the provided JSON schema.";
    }
}
