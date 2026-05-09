using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;

namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IRagService
    {
        // Q&A
        Task<RagPrompt> ScoredBinaryQuestion(RagPromptRequest request);
        Task<RagPrompt> SimpleRagQuery(RagPromptRequest request);
        Task<RagPrompt> SimpleSmartQuery(SimplePromptRequest request);
        // Chat Turns with rag
        Task<ChatPrompt> RagChatPrompt(RagChatRequest request);

        Task<Dictionary<string, ChromaQuery>> QueryCollections(List<string> names, string text, int resultsNumber, double? minDistance, Dictionary<string, object> filters);

        Task<string> QueryCollections(string text, List<string> names, int resultsNumber, double? minDistance, Dictionary<string, object> filters);
    }
}
