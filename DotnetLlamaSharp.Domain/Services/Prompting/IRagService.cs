using Dotnet.OllamaSharp.LameChain.SDK.Models.Response;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Models.Request.Prompting;

namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IRagService
    {
        Task<RagPrompt> SimpleRagQuery(RagCommandRequest request, SmartRagSettings smartSettings = null);
        Task<RagPrompt> SimpleSmartQuery(SimpleSmartQueryRequest request);
        Task<ChainResult> SmartRagChain(SimpleCommandRequest request);
        // Chat Turns with rag
        Task<ChatPrompt> RagChatPrompt(RagChatCommandRequest request);

        Task<Dictionary<string, ChromaQuery>> QueryCollections(List<string> names, string text, int resultsNumber, double? maxDistance, Dictionary<string, object> filters);
        Task<string> QueryCollections(string text, List<string> names, int resultsNumber, double? maxDistance, Dictionary<string, object> filters);
        Task<List<string>> GetChromaCollectionChoices(bool withChatCollections = false);
    }
}
