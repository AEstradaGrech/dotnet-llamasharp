using DotnetLlamaSharp.Domain.Repositories.Chroma;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases
{
    /// <summary>
    /// All ChromaPromptCommands must have a db message name. Atomic Values are DbPromptCommands so they require Chroma messages for now
    /// v2 -> Atomic Values with hardcoded string message in case there is no sysmessage or dbInstructionMessage
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ChromaPromptCommand<T> : BasePromptCommand<T>
    {
        private readonly IChromaSysChunksRepository _repo;
        protected readonly string _messageName;
        public ChromaPromptCommand() : base(){ }
        public ChromaPromptCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null) : base(guidanceMessage) 
        { 
            _repo = repo;
            _messageName = dbMessageName;
        }

       
        protected override async Task<string> getPromptInstruction()
        {
            var sysMessageChunk = await _repo.GetByName("system-messages", _messageName);

            if (string.IsNullOrEmpty(sysMessageChunk.Text))
                throw new InvalidDataException($"{nameof(ChromaPromptCommand<T>)} >> no system message text has been found for message: '{_messageName}'");

            return string.IsNullOrEmpty(_systemMessage) ? sysMessageChunk.Text : $"{_systemMessage}\n{sysMessageChunk.Text}";
        }
    }
}
