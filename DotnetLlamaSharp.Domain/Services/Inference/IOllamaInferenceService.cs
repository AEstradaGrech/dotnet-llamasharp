using DotnetLlamaSharp.Domain.Models.Enums;
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

        Task<T> StructuredPrompt<T>(GenerateRequest request, int validations = 0, EPromptValidation type = EPromptValidation.REVIEW_ONLY) where T : class;
        Task<T> StructuredPrompt<T>(string prompt, string? systemGuidance = null, string? jsonModel = null) where T : class;
    }
}
