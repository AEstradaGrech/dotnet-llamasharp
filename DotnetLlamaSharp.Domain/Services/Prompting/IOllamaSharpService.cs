using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using MicrosoftAI = Microsoft.Extensions.AI;

namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IOllamaSharpService
    {
        Task<ChatPrompt> SimplePrompt(SimplePromptRequest request);
        IAsyncEnumerable<GenerateResponseStream?> SimplePromptStream(SimplePromptRequest request);
        Task<ChatPrompt> ChatPrompt(ChatPromptRequest request, bool bWithSysmsgUpdate = false);
        IAsyncEnumerable<ChatResponseStream?> ChatPromptStream(ChatPromptRequest request, bool bWithSysmsgUpdate = false);
        Task<EmbedResponse> GetOllamaClientEmbeddings(EmbedRequest request);

        Task<ChatPrompt> BooleanQuestion(SimplePromptRequest request);
        Task<TEnum?> EnumChoice<TEnum>(string prompt, string? instruction = null) where TEnum : struct, Enum;
        Task<string> StringChoice(string prompt, List<string> choices, string? instruction = null);
    }
}
