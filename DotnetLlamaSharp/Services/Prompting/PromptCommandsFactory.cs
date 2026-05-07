using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Prompting;

namespace DotnetLlamaSharp.Services.Prompting
{
    public class PromptCommandsFactory : IPromptCommandsFactory
    {
        private readonly IChromaSysChunksRepository _systemRepo;
        public PromptCommandsFactory(IChromaSysChunksRepository sysRepo) { _systemRepo = sysRepo; }
        public TCommand GetCommand<TCommand, TResult>(string? systemMessage = null) where TCommand : BasePromptCommand<TResult>, new()
            => Activator.CreateInstance(typeof(TCommand), systemMessage) as TCommand;

        public TCommand GetCommand<TCommand, TResult>(string dbMessageName, string? systemMessage = null) where TCommand : DbPromptCommand<TResult>, new()
            => Activator.CreateInstance(typeof(TCommand), _systemRepo, dbMessageName, systemMessage) as TCommand;

        public EnumPromptCommand<TEnum> GetEnumChoiceCommand<TEnum>(string dbMessageName, string? systemMessage = null) where TEnum : struct, Enum
            => Activator.CreateInstance(typeof(EnumPromptCommand<TEnum>), _systemRepo, dbMessageName, systemMessage) as EnumPromptCommand<TEnum>;

        public BoolPromptCommand GetBoolPromptCommand(string dbMessageName, string? systemMessage = null)
            => Activator.CreateInstance(typeof(BoolPromptCommand), _systemRepo, dbMessageName, systemMessage) as BoolPromptCommand;

        public MessagePromptCommand GetMessagePromptCommand(string? systemMessage = null)
            => Activator.CreateInstance(typeof(MessagePromptCommand), systemMessage) as MessagePromptCommand;
    }
}
