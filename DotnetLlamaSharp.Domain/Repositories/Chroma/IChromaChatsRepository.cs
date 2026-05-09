using DotnetLlamaSharp.Domain.Models.Entities.Chroma;

namespace DotnetLlamaSharp.Domain.Repositories.Chroma
{
    public interface IChromaChatsRepository : IChromaRepository<ChromaChatsCollection, ChromaChatChunk>
    {
        Task<ChromaChatsCollection> InitCollection(string agentName, string userName, string? embeddingModel, int? dimensions, string? description = null);
        Task<ChromaChatChunk> GetCurrentSessionChunk(string collection);
        Task<List<ChromaChatChunk>> GetSessionChunks(string collectionName, string sessionId, bool isDescendingOrder = false, int? results = null);
        Task<List<ChromaChatChunk>> GetCollectionSessions(string collectionName);
        Task<List<ChromaQueryChunk>> QueryCollection(string collection, ReadOnlyMemory<float> queryEmbedding, int resultsNumber, bool withSessions = false, Dictionary<string, object> filters = null);
    }
}
