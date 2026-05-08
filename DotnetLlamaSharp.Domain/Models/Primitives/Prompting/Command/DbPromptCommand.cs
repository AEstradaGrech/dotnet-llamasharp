using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class DbPromptCommand<T> : BasePromptCommand<T>
    {
        private readonly IChromaSysChunksRepository _repo;
        protected readonly string _messageName;
        public DbPromptCommand() : base(){ }
        public DbPromptCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null) : base(guidanceMessage) 
        { 
            _repo = repo;
            _messageName = dbMessageName;
        }

        public override Task<T> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            throw new NotImplementedException();
        }

        protected override async Task<string> getPromptInstruction()
        {
            var sysMessageChunk = await _repo.GetByName("system-messages", _messageName);

            if (string.IsNullOrEmpty(sysMessageChunk.Text))
                throw new InvalidDataException($"{nameof(DbPromptCommand<T>)} >> no system message text has been found for message: '{_messageName}'");

            return string.IsNullOrEmpty(_systemMessage) ? sysMessageChunk.Text : $"{_systemMessage}\n{sysMessageChunk.Text}";
        }
    }
}
