

using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;

namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IPromptCommandsFactory
    {
        TCommand GetCommand<TCommand, TResult>(string? systemMessage = null) where TCommand : BasePromptCommand<TResult>, new();
        MessagePromptCommand GetMessagePromptCommand(string? systemMessage = null);
        TCommand GetCommand<TCommand, TResult>(string dbMessageName, string? guidanceMessage = null) where TCommand : ChromaPromptCommand<TResult>, new();
        EnumPromptCommand<TEnum> GetEnumChoiceCommand<TEnum>(string dbMessageName, string? guidanceMessage = null) where TEnum : struct, Enum;
        BoolPromptCommand GetBoolPromptCommand(string dbMessageName, string? guidanceMessage = null);
        StringChoiceCommand GetStringChoiceCommand(string dbMessageName, string? guidanceMessage = null);
        NumericPromptCommand GetNumericPromptCommand(string dbMessageName, string? guidanceMessage = null);
        MultiChoiceCommand GetMultiChoiceCommand(string dbMessageName, string? guidanceMessage = null);        
    }
}
