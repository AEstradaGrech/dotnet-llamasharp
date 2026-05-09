using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Services.Inference;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    /// <summary>
    /// Atomic Value Commands returns simple values (c# structs) mapped from a structured class json (return type IS NOT a class)
    /// Simplest form of structured output command (ensures return type IS a class, so the same structured class json is returned as it is). 
    /// Use with / without system guidance message, returns StructuredOutput response.
    /// HAS NOT Chroma dependancy
    /// </summary>
    public class GuidedStructuredPrompt<TJson> : BasePromptCommand<TJson> where TJson : class
    {
        public GuidedStructuredPrompt() : base() { }
        public GuidedStructuredPrompt(string? systemMessage = null, CommandSettings? settings = null) : base(systemMessage, settings){ }
        public override async Task<TJson> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
            => await ollama.StructuredPrompt<TJson>(request.Prompt, await getPromptInstruction(), request.Model);

        protected override async Task<string> getPromptInstruction() => string.IsNullOrEmpty(_systemMessage) ? string.Empty : _systemMessage; 
    }
}
