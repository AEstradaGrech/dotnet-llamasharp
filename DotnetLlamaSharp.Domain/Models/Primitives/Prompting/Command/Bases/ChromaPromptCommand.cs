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
        protected readonly string _defaultMessage = string.Empty;
        public ChromaPromptCommand() : base(){ }
        public ChromaPromptCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) : base(guidanceMessage, settings) 
        { 
            _repo = repo;
            _messageName = dbMessageName;
        }

        protected virtual string getDefaultInstruction() => _defaultMessage;
        protected override async Task<string> getPromptInstruction()
        {
            try
            {
                var sysMessageChunk = await _repo.GetByName("system-messages", _messageName);

                if (string.IsNullOrEmpty(sysMessageChunk.Text))
                    throw new InvalidDataException($"{nameof(ChromaPromptCommand<T>)} >> no system message text has been found for message: '{_messageName}'");

                return string.IsNullOrEmpty(_systemMessage) ? sysMessageChunk.Text : $"{_systemMessage}\n{sysMessageChunk.Text}";
            }
            catch (Exception ex)
            {
                var defaultMessage = getDefaultInstruction();

                if (string.IsNullOrEmpty(defaultMessage))
                    throw ex;

                return defaultMessage;
            }
            
        }
    }
}
