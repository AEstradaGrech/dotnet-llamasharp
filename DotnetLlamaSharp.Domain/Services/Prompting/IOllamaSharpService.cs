using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Request;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using MicrosoftAI = Microsoft.Extensions.AI;

namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IOllamaSharpService
    {
        Task<ChatPrompt> SimplePrompt(SimplePromptRequest request);
        Task<ChatPrompt> ChatPrompt(ChatPromptRequest request, bool bWithSysmsgUpdate = false);
        Task<EmbedResponse> GetOllamaClientEmbeddings(EmbedRequest request);

        //Task<ChatPrompt> BooleanQuestion(SimplePromptRequest request);
        //Task<TEnum?> EnumChoice<TEnum>(string prompt, string? instruction = null) where TEnum : struct, Enum;
        //Task<string> StringChoice(string prompt, List<string> choices, string? model, string? instruction = null);
        //Task<List<string>> MultiChoice(string prompt, List<string> choices, int maxChoices, string? instruction = null);


        //Task<TResult> GuidedPromptCommand<TCommand, TRequest, TResult>(TRequest request, string? instruction = null) where TCommand : BasePromptCommand<TResult>, new() where TRequest : PromptCommandRequest;
        //Task<TResult> DbPromptCommand<TCommand, TRequest, TResult>(TRequest request, string dbInstructionName, string? instruction = null) where TCommand : ChromaPromptCommand<TResult> , new() where TRequest : PromptCommandRequest;
    }
}
