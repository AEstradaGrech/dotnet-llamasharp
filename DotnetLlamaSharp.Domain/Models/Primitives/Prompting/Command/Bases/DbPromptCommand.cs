using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;

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
    public abstract class DbPromptCommand<T> : BasePromptCommand<T>
    {
        protected readonly IChromaSysChunksRepository _repo;
        protected readonly string _messageName;
        protected readonly string _defaultMessage = string.Empty;
        public DbPromptCommand() : base() { }
        public DbPromptCommand(IOllamaInferenceService ollama) : base(ollama){ }
        public DbPromptCommand(IOllamaInferenceService ollama, string? systemMessage = null, CommandSettings? settings = null) : base(ollama, systemMessage, settings) { }
        //                                                     Func<string,string> getDbMessage() <- se enchufa una lambda y se desacoplan los DBCommand
        //                                                     ConnectorsFactory --> Func<IChromaRepo, string>Get() | Func<IMongoRepo,string> a medida que añado integraciones, aumento la factoria
        //                                                     ISysMessageable -> interfaz comun para que todos los connectors tengan un mismo "GetSysMessage(name)", que es lo mas comun (casi lo unico que hace el repo)
        //                                                     SysMessageLambda para AtomicValues (se quita la dependencia a Chroma, funcionan con default o lo que venga en la Lambda)
        //                                                     Commandos que reciben un repo (dependencia) pasan a ser DbXCommands
        //                                                     v3 -> Factoria Cool para reutilizar command de DbX en DbA | B | C
        public DbPromptCommand(IOllamaInferenceService ollama, IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) : base(ollama, guidanceMessage, settings) 
        { 
            _repo = repo;
            _messageName = dbMessageName;
        }

        //Validator that might read the instruction from Chroma or use the defaultInstruction
        protected override JsonOutputRefinerCommand<T> validatorFor<T>() where T : class => new JsonOutputRefinerCommand<T>(_ollama, _repo, _messageName, null, _settings);
        protected virtual string getDefaultInstruction() => _defaultMessage;
        protected override async Task<string> getPromptInstruction(string? guidanceMessage = null)
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
                        throw new InvalidDataException($"{nameof(DbPromptCommand<T>)} >> no system message text has been found for message: '{_messageName}'");

                    systemMessage = sysMessageChunk.Text;
                }
                // if no db message and/or !defaultInstruction throw ex & try defaultInstruction (if db fail) + _system (request guidance msg)
                if (string.IsNullOrEmpty(systemMessage))
                    throw new InvalidDataException($"{nameof(DbPromptCommand<T>)} >> NO SYSTEM MESSAGE FOUND >> TRYING DEFAULT MESSAGES");
                
                if (!string.IsNullOrEmpty(guidanceMessage))
                    systemMessage = $"{systemMessage}\n\n{guidanceMessage}";

                // default | dbSys
                // guidance + (default | dbSys)
                // instruction + (default | dbSys)
                // instruction + guidance + (default | dbSys)
                return string.IsNullOrEmpty(_systemMessage) ? systemMessage : $"{_systemMessage}\n{systemMessage}";
            }
            catch (Exception ex)
            {
                // default hardcoded message (if any) + any guidance message from constructor
                var defaultMessage = $"{(string.IsNullOrEmpty(_systemMessage) ? "" : $"{_systemMessage}\n")}{getDefaultInstruction()}";

                if (string.IsNullOrEmpty(defaultMessage))
                    throw ex;

                if (!string.IsNullOrEmpty(guidanceMessage))
                    defaultMessage = $"{defaultMessage}\n{guidanceMessage}";

                return defaultMessage;
            }
            
        }
    }
}
