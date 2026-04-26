using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Domain.Services.Inference
{
    public interface IOllamaInferenceService
    {
        Task<Message> SimplePrompt(GenerateRequest request);
        IAsyncEnumerable<GenerateResponseStream?> SimplePromptStream(GenerateRequest request);
        Task<Message> ChatPrompt(ChatRequest request);
        IAsyncEnumerable<ChatResponseStream?> ChatPromptStream(ChatRequest request);
        Task<EmbedResponse> GetEmbeddings(EmbedRequest request);
    }
}
