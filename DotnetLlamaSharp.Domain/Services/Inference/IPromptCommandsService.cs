using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;

namespace DotnetLlamaSharp.Domain.Services.Inference
{
    public interface IPromptCommandsService
    {
        Task<TEnum?> EnumChoice<TEnum>(string prompt, string? model = null, string? instruction = null) where TEnum : struct, Enum;
        Task<bool> BooleanChoice(string prompt, string? model = null, string? instruction = null);
        Task<string> StringBoolChoice(string prompt, string? model = null, string? instruction = null);
        Task<string> StringChoice(string prompt, List<string> choices, string? model, string? instruction = null);
        Task<List<string>> MultiChoice(string prompt, List<string> choices, int maxChoices, string? model = null, string? instruction = null);
        Task<float?> NumericResult(string prompt, string? model = null, string? instruction = null);
        Task<ScoredBoolResponse> ScoredBool(string prompt, string? model = null, string? instrucion = null);
        Task<ScoredStringChoice> ScoredChoice(List<string> choices, string prompt, string? model = null, string? instrucion = null);
        Task<TResult> GuidedPromptCommand<TCommand, TRequest, TResult>(TRequest request, string? instruction = null) where TCommand : BasePromptCommand<TResult>, new() where TRequest : PromptCommandRequest;
        Task<TResult> DbPromptCommand<TCommand, TRequest, TResult>(TRequest request, string dbInstructionName, string? instruction = null) where TCommand : ChromaPromptCommand<TResult>, new() where TRequest : PromptCommandRequest;
    }
}
