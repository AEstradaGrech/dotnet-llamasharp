

using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command;
using DotnetLlamaSharp.Domain.Repositories.Chroma;

namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IPromptCommandsFactory
    {
        TCommand GetCommand<TCommand, TResult>(string? systemMessage = null) where TCommand : BasePromptCommand<TResult>, new();
        MessagePromptCommand GetMessagePromptCommand(string? systemMessage = null);
        TCommand GetCommand<TCommand, TResult>(string dbMessageName, string? systemMessage = null) where TCommand : DbPromptCommand<TResult>, new();
        EnumPromptCommand<TEnum> GetEnumChoiceCommand<TEnum>(string dbMessageName, string? systemMessage = null) where TEnum : struct, Enum;
        BoolPromptCommand GetBoolPromptCommand(string dbMessageName, string? systemMessage = null);
        
    }
}
