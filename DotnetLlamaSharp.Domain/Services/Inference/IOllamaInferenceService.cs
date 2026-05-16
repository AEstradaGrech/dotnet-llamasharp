using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Domain.Services.Inference
{
    public interface IOllamaInferenceService
    {
        Task<Message> GeneratePrompt(GenerateRequest request);
        IAsyncEnumerable<GenerateResponseStream?> GeneratePromptStream(GenerateRequest request);
        Task<Message> ChatPrompt(ChatRequest request);
        IAsyncEnumerable<ChatResponseStream?> ChatPromptStream(ChatRequest request);
        Task<EmbedResponse> GetEmbeddings(EmbedRequest request);

        Task<T> CommandPrompt<T>(GenerateRequest request, int validations = 0, EPromptValidation type = EPromptValidation.REVIEW_ONLY, JsonOutputRefinerCommand<T> validator = null) where T : class;
        Task<T> StructuredPrompt<T>(string prompt, string? systemGuidance = null, string? jsonModel = null) where T : class;
    }
}
