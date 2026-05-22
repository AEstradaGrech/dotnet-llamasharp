

using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;

namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IPromptCommandsFactory
    {
        TCommand GetCommand<TCommand, TResult>(string? systemMessage = null, PromptSettings? settings = null) where TCommand : BasePromptCommand<TResult>, new();
        MessagePromptCommand GetMessagePromptCommand(string? systemMessage = null, PromptSettings? settings = null);
        TCommand GetChromaCommand<TCommand, TResult>(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null) where TCommand : DbPromptCommand<TResult>, new();
        TCommand GetDbCommand<TCommand, TResult>(string source, string messageName, Func<string, string, Task<string>> retrieverLambda, string? guidanceMessage = null, PromptSettings? settings = null) where TCommand : DbPromptCommand<TResult>, new();
        EnumPromptCommand<TEnum> GetEnumChoiceCommand<TEnum>(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null) where TEnum : struct, Enum;
        BoolPromptCommand GetBoolPromptCommand(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null);
        StringChoiceCommand GetStringChoiceCommand(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null);
        NumericPromptCommand GetNumericPromptCommand(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null);
        MultiChoiceCommand GetMultiChoiceCommand(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null);

        IPrompteable<TResult> GetAsPrompteable<TCommand, TResult>(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null);
        IPrompteable<ChatMessage> GetPrompteableMessage(string? systemMessage = null, PromptSettings? settings = null);

        IJsoneable GetAsJsoneable<TCommand, TResult>(string instruction) where TCommand : BasePromptCommand<TResult>, new();
        IJsoneable GetAsJsoneable<TCommand, TResult>(string instruction, PromptSettings? settings = null) where TCommand : BasePromptCommand<TResult>, new();
        IJsoneable GetAsJsoneable<TCommand, TResult>(string? instruction = null, string? dbMessageName = null, PromptSettings? settings = null) where TCommand : DbPromptCommand<TResult>, new();
    }
}
