using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Services.Inference;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{

    /// <summary>
    /// Simplest form of Structured Output that depends on Chroma System Chunks to read the instruction message
    /// The Structured Output response class is returned as it is (as opposed to an 'atomic value' command, that maps the class result to a primitive type)
    /// </summary>
    /// <typeparam name="TJson"></typeparam>
    public class DbStructuredPrompt<TJson> : DbPromptCommand<TJson> where TJson : class
    {
        public DbStructuredPrompt() : base() { }
        public DbStructuredPrompt(IOllamaInferenceService ollama, string messageSourceName, string messageName, Func<string, string, Task<string>> retriever, string? guidanceMessage = null, CommandSettings? settings = null) 
            : base(ollama, messageSourceName, messageName, retriever, guidanceMessage, settings) { }

        public override async Task<TJson> Prompt(PromptCommandRequest request)
            => await _ollama.CommandPrompt<TJson>(await getGenerateRequest(request), _settings.CommandValidations, _settings.ValidationType, validatorFor<TJson>());
    }
}
