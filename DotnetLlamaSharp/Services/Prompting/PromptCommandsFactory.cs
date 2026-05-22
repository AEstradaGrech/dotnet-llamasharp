using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using DotnetLlamaSharp.Domain.Services.Prompting;

namespace DotnetLlamaSharp.Services.Prompting
{
    public class PromptCommandsFactory : IPromptCommandsFactory
    {
        private readonly IChromaSysChunksRepository _systemRepo;
        private readonly IOllamaInferenceService _ollama;
        public PromptCommandsFactory(IChromaSysChunksRepository sysRepo, IOllamaInferenceService ollama) 
        { 
            _systemRepo = sysRepo;
            _ollama = ollama;
        }
        
        //Generic factory methods
        public TCommand GetCommand<TCommand, TResult>(string? systemMessage = null, PromptSettings? settings = null) where TCommand : BasePromptCommand<TResult>, new()
            => Activator.CreateInstance(typeof(TCommand), _ollama, systemMessage, settings) as TCommand;

        public TCommand GetChromaCommand<TCommand, TResult>(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null) where TCommand : DbPromptCommand<TResult>, new()
            => Activator.CreateInstance(typeof(TCommand), _ollama, _systemRepo, dbMessageName, guidanceMessage, settings) as TCommand;

        //Domain object command helpers
        public MessagePromptCommand GetMessagePromptCommand(string? systemMessage = null, PromptSettings? settings = null)
            => Activator.CreateInstance(typeof(MessagePromptCommand), _ollama, systemMessage, settings) as MessagePromptCommand;

        //Atomic value command helpers
        public EnumPromptCommand<TEnum> GetEnumChoiceCommand<TEnum>(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null) where TEnum : struct, Enum
            => Activator.CreateInstance(typeof(EnumPromptCommand<TEnum>), _ollama, _systemRepo, dbMessageName, guidanceMessage, settings) as EnumPromptCommand<TEnum>;

        public BoolPromptCommand GetBoolPromptCommand(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null)
            => Activator.CreateInstance(typeof(BoolPromptCommand), _ollama, _systemRepo, dbMessageName, guidanceMessage, settings) as BoolPromptCommand;

        public StringChoiceCommand GetStringChoiceCommand(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null)
            => Activator.CreateInstance(typeof(StringChoiceCommand), _ollama, _systemRepo, dbMessageName, guidanceMessage, settings) as StringChoiceCommand;

        public MultiChoiceCommand GetMultiChoiceCommand(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null)
            => Activator.CreateInstance(typeof(MultiChoiceCommand), _ollama, _systemRepo, dbMessageName, guidanceMessage, settings) as MultiChoiceCommand;

        public NumericPromptCommand GetNumericPromptCommand(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null)
            => Activator.CreateInstance(typeof(NumericPromptCommand), _ollama, _systemRepo, dbMessageName, guidanceMessage, settings) as NumericPromptCommand;

        public IPrompteable<TResult> GetAsPrompteable<TCommand, TResult>(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null)
            => Activator.CreateInstance(typeof(TCommand), _ollama, _systemRepo, dbMessageName, guidanceMessage, settings) as IPrompteable<TResult>;

        public IPrompteable<ChatMessage> GetPrompteableMessage(string? systemMessage = null, PromptSettings? settings = null)
            => Activator.CreateInstance(typeof(MessagePromptCommand), _ollama, systemMessage, settings) as IPrompteable<ChatMessage>;

        public IJsoneable GetAsJsoneable<TCommand, TResult>(string instruction) where TCommand : BasePromptCommand<TResult>, new()
           => GetCommand<TCommand, TResult>(instruction, settings: null);
        public IJsoneable GetAsJsoneable<TCommand, TResult>(string? instruction = null, PromptSettings? settings = null) where TCommand : BasePromptCommand<TResult>, new()
           => GetCommand<TCommand, TResult>(instruction, settings);
        public IJsoneable GetAsJsoneable<TCommand, TResult>(string? instruction = null, string? dbMessageName = null, PromptSettings? settings = null) where TCommand : DbPromptCommand<TResult>, new()
           =>  GetChromaCommand<TCommand, TResult>(dbMessageName, instruction, settings);

        public TCommand GetDbCommand<TCommand, TResult>(string source, string messageName, Func<string, string, Task<string>> retrieverLambda, string? guidanceMessage = null, PromptSettings? settings = null) where TCommand : DbPromptCommand<TResult>, new()
            => Activator.CreateInstance(typeof(TCommand), _ollama, source, messageName, retrieverLambda, guidanceMessage, settings) as TCommand;
    }
}
