using DotnetLlamaSharp.Domain.Repositories.Chroma;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases
{
    /// <summary>
    /// All ChromaPromptCommands must have a db message name. Atomic Values are DbPromptCommands that 
    /// may read an instruction message from Chroma or use a default hardcoded message. 
    /// The also accept some extra guidance instruction from the http request that is stored as _systemMessage
    /// So they can be used:
    ///     - with guidance message only
    ///     - default hardcoded only
    ///     - default hardcoded + guidance
    ///     - chroma stored only
    ///     - chroma stored + guidance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ChromaPromptCommand<T> : BasePromptCommand<T>
    {
        protected readonly IChromaSysChunksRepository _repo;
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
                //if prefer default instruction, try get instruction
                string systemMessage = string.Empty;

                if(_settings.UseDefaultCommandMessage)
                    systemMessage = getDefaultInstruction();
                // else try db msg
                if(string.IsNullOrEmpty(systemMessage))
                {
                    var sysMessageChunk = await _repo.GetByName("system-messages", _messageName);

                    if (string.IsNullOrEmpty(sysMessageChunk.Text))
                        throw new InvalidDataException($"{nameof(ChromaPromptCommand<T>)} >> no system message text has been found for message: '{_messageName}'");

                    systemMessage = sysMessageChunk.Text;
                }
                // if no db message and/or !defaultInstruction throw ex & try defaultInstruction (if db fail) + _system (request guidance msg)
                if (string.IsNullOrEmpty(systemMessage))
                    throw new InvalidDataException($"{nameof(ChromaPromptCommand<T>)} >> NO SYSTEM MESSAGE FOUND >> TRYING DEFAULT MESSAGES");

                return string.IsNullOrEmpty(_systemMessage) ? systemMessage : $"{_systemMessage}\n{systemMessage}";
            }
            catch (Exception ex)
            {
                // default hardcoded message (if any) + any guidance message from constructor
                var defaultMessage = $"{getDefaultInstruction()}{(string.IsNullOrEmpty(_systemMessage) ? "" : $"\n{_systemMessage}")}";

                if (string.IsNullOrEmpty(defaultMessage))
                    throw ex;

                return defaultMessage;
            }
            
        }
    }
}
