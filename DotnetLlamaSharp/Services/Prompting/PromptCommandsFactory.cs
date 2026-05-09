using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Prompting;

namespace DotnetLlamaSharp.Services.Prompting
{
    public class PromptCommandsFactory : IPromptCommandsFactory
    {
        private readonly IChromaSysChunksRepository _systemRepo;
        public PromptCommandsFactory(IChromaSysChunksRepository sysRepo) { _systemRepo = sysRepo; }
        
        //Generic factory methods
        public TCommand GetCommand<TCommand, TResult>(string? systemMessage = null, PromptSettings? settings = null) where TCommand : BasePromptCommand<TResult>, new()
            => Activator.CreateInstance(typeof(TCommand), systemMessage, settings) as TCommand;

        public TCommand GetCommand<TCommand, TResult>(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null) where TCommand : ChromaPromptCommand<TResult>, new()
            => Activator.CreateInstance(typeof(TCommand), _systemRepo, dbMessageName, guidanceMessage, settings) as TCommand;

        //Domain object command helpers
        public MessagePromptCommand GetMessagePromptCommand(string? systemMessage = null, PromptSettings? settings = null)
            => Activator.CreateInstance(typeof(MessagePromptCommand), systemMessage, settings) as MessagePromptCommand;

        //Atomic value command helpers
        public EnumPromptCommand<TEnum> GetEnumChoiceCommand<TEnum>(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null) where TEnum : struct, Enum
            => Activator.CreateInstance(typeof(EnumPromptCommand<TEnum>), _systemRepo, dbMessageName, guidanceMessage, settings) as EnumPromptCommand<TEnum>;

        public BoolPromptCommand GetBoolPromptCommand(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null)
            => Activator.CreateInstance(typeof(BoolPromptCommand), _systemRepo, dbMessageName, guidanceMessage, settings) as BoolPromptCommand;

        public StringChoiceCommand GetStringChoiceCommand(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null)
            => Activator.CreateInstance(typeof(StringChoiceCommand), _systemRepo, dbMessageName, guidanceMessage, settings) as StringChoiceCommand;

        public MultiChoiceCommand GetMultiChoiceCommand(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null)
            => Activator.CreateInstance(typeof(MultiChoiceCommand), _systemRepo, dbMessageName, guidanceMessage, settings) as MultiChoiceCommand;

        public NumericPromptCommand GetNumericPromptCommand(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null)
            => Activator.CreateInstance(typeof(NumericPromptCommand), _systemRepo, dbMessageName, guidanceMessage, settings) as NumericPromptCommand;
    }
}
