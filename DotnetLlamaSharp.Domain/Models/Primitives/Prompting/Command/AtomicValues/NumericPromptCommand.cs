using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues
{
    // Esto acopla los atomic command a Chroma
    // Deberia usarse solo el guidance message como system y punto
    // ChromaNumericPrompt : NumericPromptCommand : BasePromptCommand<float>
    // añade constructor a repo y hace override de DbPromptCommand.getInstructionMessage() 
    public class NumericPromptCommand : DbPromptCommand<float?>
    {
        public NumericPromptCommand() : base() { }
        public NumericPromptCommand(IOllamaInferenceService ollama) : base(ollama) { }
        public NumericPromptCommand(IOllamaInferenceService ollama, IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) 
            : base(ollama, repo, dbMessageName, guidanceMessage, settings) { }

        public override async Task<float?> Prompt(PromptCommandRequest request)
        {
            var response = await _ollama.CommandPrompt<NumericResponse>(await getGenerateRequest(request), _settings.CommandValidations, _settings.ValidationType, validatorFor<NumericResponse>());

            return response.Result;
        }

        protected override string getDefaultInstruction()
            => "Analyze the user request and output your 'numeric result' according to the provided JSON schema, ONLY in case it can be answered with a number. If the question can't be answered with a number, output a null value according to the JSON schema.";
        /*Analyze the user request and, in case it can be answered with a number, output your numeric response according to the provided JSON schema. If the question can't be answered with a number, output null according to the JSON schema*/
    }
}
