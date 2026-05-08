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
    public class ChromaStructuredPrompt<TJson> : ChromaPromptCommand<TJson> where TJson : class
    {
        public override async Task<TJson> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
            => await ollama.StructuredPrompt<TJson>(request.Prompt, await getPromptInstruction(), request.Model);
    }
}
