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
        public TCommand GetCommand<TCommand, TResult>(string? systemMessage = null) where TCommand : BasePromptCommand<TResult>, new()
            => Activator.CreateInstance(typeof(TCommand), systemMessage) as TCommand;

        public TCommand GetCommand<TCommand, TResult>(string dbMessageName, string? guidanceMessage = null) where TCommand : ChromaPromptCommand<TResult>, new()
            => Activator.CreateInstance(typeof(TCommand), _systemRepo, dbMessageName, guidanceMessage) as TCommand;

        //Domain object command helpers
        public MessagePromptCommand GetMessagePromptCommand(string? systemMessage = null)
            => Activator.CreateInstance(typeof(MessagePromptCommand), systemMessage) as MessagePromptCommand;

        //Atomic value command helpers
        public EnumPromptCommand<TEnum> GetEnumChoiceCommand<TEnum>(string dbMessageName, string? guidanceMessage = null) where TEnum : struct, Enum
            => Activator.CreateInstance(typeof(EnumPromptCommand<TEnum>), _systemRepo, dbMessageName, guidanceMessage) as EnumPromptCommand<TEnum>;

        public BoolPromptCommand GetBoolPromptCommand(string dbMessageName, string? guidanceMessage = null)
            => Activator.CreateInstance(typeof(BoolPromptCommand), _systemRepo, dbMessageName, guidanceMessage) as BoolPromptCommand;

        public StringChoiceCommand GetStringChoiceCommand(string dbMessageName, string? guidanceMessage = null)
            => Activator.CreateInstance(typeof(StringChoiceCommand), _systemRepo, dbMessageName, guidanceMessage) as StringChoiceCommand;

        public MultiChoiceCommand GetMultiChoiceCommand(string dbMessageName, string? guidanceMessage = null)
            => Activator.CreateInstance(typeof(MultiChoiceCommand), _systemRepo, dbMessageName, guidanceMessage) as MultiChoiceCommand;

        public NumericPromptCommand GetNumericPromptCommand(string dbMessageName, string? guidanceMessage = null)
            => Activator.CreateInstance(typeof(NumericPromptCommand), _systemRepo, dbMessageName, guidanceMessage) as NumericPromptCommand;
    }
}
