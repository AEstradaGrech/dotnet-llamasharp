using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;

namespace DotnetLlamaSharp.Domain.Services.Inference
{
    public interface IPromptCommandsService
    {
        Task<TEnum?> EnumChoice<TEnum>(string prompt, string? instruction = null, CommandSettings settings = null) where TEnum : struct, Enum;
        Task<bool> BooleanChoice(string prompt, string? instruction = null, CommandSettings settings = null);
        Task<string> StringBoolChoice(string prompt, string? instruction = null, CommandSettings settings = null);
        Task<string> StringChoice(string prompt, List<string> choices, string? instruction = null, CommandSettings settings = null);
        Task<List<string>> MultiChoice(string prompt, List<string> choices, int maxChoices, string? instruction = null, CommandSettings settings = null);
        Task<float?> NumericResult(string prompt, string? instruction = null, CommandSettings settings = null);
        Task<ScoredBoolResponse> ScoredBool(string prompt, string? instrucion = null, CommandSettings settings = null);
        Task<ScoredStringChoice> ScoredChoice(List<string> choices, string prompt, string? instrucion = null, CommandSettings settings = null);
        Task<TResult> GuidedPromptCommand<TCommand, TRequest, TResult>(TRequest request, string? instruction = null, CommandSettings settings = null) where TCommand : BasePromptCommand<TResult>, new() where TRequest : PromptCommandRequest;
        Task<TResult> GuidedPromptCommand<TRequest, TResult>(TRequest request, string? instruction = null, CommandSettings settings = null) where TRequest : PromptCommandRequest where TResult : class;
        Task<TResult> DbPromptCommand<TCommand, TRequest, TResult>(TRequest request, string dbInstructionName, string? instruction = null, CommandSettings settings = null) where TCommand : DbPromptCommand<TResult>, new() where TRequest : PromptCommandRequest;
        Task<TResult> DbPromptCommand<TRequest, TResult>(TRequest request, string dbInstructionName, string? instruction = null, CommandSettings settings = null) where TRequest : PromptCommandRequest where TResult : class;

        TCommand GetCommand<TCommand, TResult>(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null) where TCommand : DbPromptCommand<TResult>, new();


    }
}
