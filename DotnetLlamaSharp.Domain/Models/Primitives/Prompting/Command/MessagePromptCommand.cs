using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class MessagePromptCommand : BasePromptCommand<ChatMessage>
    {
        public MessagePromptCommand(string? systemMessage) : base(systemMessage) { }

        public override async Task<ChatMessage> Prompt(IOllamaInferenceService ollama, PromptCommandRequest requestl)
        {
            return new ChatMessage("test", "this is a test");
        }

        protected override async Task<string> getPromptInstruction() => _systemMessage == null ? string.Empty : _systemMessage;
    }
}
