using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using MicrosoftAI = Microsoft.Extensions.AI;

namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IOllamaSharpService
    {
        Task<ChatPromptResponse> SimplePrompt(ChatPromptRequest request);
        IAsyncEnumerable<GenerateResponseStream?> SimplePromptStream(ChatPromptRequest request);
        Task<ChatPromptResponse> ChatPrompt(ChatPromptRequest request, bool bWithSysmsgUpdate = false);
        IAsyncEnumerable<ChatResponseStream?> ChatPromptStream(ChatPromptRequest request, bool bWithSysmsgUpdate = false);
        Task<EmbedResponse> GetOllamaClientEmbeddings(EmbedRequest request);
    }
}
