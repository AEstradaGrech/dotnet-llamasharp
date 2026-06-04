using Dotnet.Chroma.Repositories.Models;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Interfaces.Model;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Request.Chroma;


namespace DotnetLlamaSharp.Domain.Services.Embeddings
{
    public interface IChromaService
    {
        Task<IAsyncEnumerable<string>> GetDbCollections();
        Task<ChromaFilesCollection> CreateEmptyFileCollection(CreateCollectionRequest request);
        Task<bool> DeleteCollection(string name);
        Task<ChromaChunksCollection<ChromaChunk>> GetCollection(string name);
        Task<ChromaFilesCollection> CreateCollectionFromFile(EmbedCollectionRequest request);
        Task<ChromaFilesCollection> InspectFilesCollection(string name, int startIndex = 0, int samples = 0, bool includeEmbeddings = false); // Default: all chunks from idx 0
        Task<ChromaChunk> InspectChunk(string collectionName, string id);
        Task<ChromaQuery> QueryCollection(string name, string query, int resultsNumber, Dictionary<string, object> metadataFilters);
        Task<List<ILameSearchResult>> SimilaritySearch(string index, ReadOnlyMemory<float> queryEmbeddings, int resultsNumber, Dictionary<string, object> metadataFilters);
        Task<ChromaChatsCollection> GetChatsCollection(string name, bool excludeSysmsg = true, string? sessionId = null, int pageSize = 10, int page = 0);
        Task<ChromaChatsCollection> GetCollectionSessions(string name, int pageSize = 10, int page = 0);
        Task<SysChunksCollection> CreateSystemChunksCollection(string name, string? description);
        Task<ChromaSysChunk> AddSysMessage(string collectionName, ChromaSysChunk chunk, string? version);
        Task<ChromaSysChunk> GetSysMessage(string collectionName, string name, string? version = null);
        Task<ChromaSysChunk> DeleteSysMessage(string collectionName, string name, string? version = null);
        Task<ChromaSysChunk> DeleteSysMessageById(string collectionName, string id);
        Task<ChromaSysChunk> PatchSysMessage(string collectionName, string id, string newText, Dictionary<string, object>? metas = null);

        Task<SysChunksCollection> GetSysChunksPage(string collectionName, int pageSize = 10, int page = 0, Dictionary<string, object> filters = null);
        Task<List<ChromaFilesCollection>> GetAllFileCollections();
        Task<List<ChromaChatsCollection>> GetAllChatCollections();

        //LameChain support
        // Todos los conectores implementan esto (o el usuario se hace un servicio en su api con esta firma)
        // Task<ICommandEmbeddings> QueryCollection(string collection, ReadOnlyMemory<float> queryEmbeddings, int results, Dictionary<string, object> filters);
        Task<string> GetSystemInstruction(string collectionName, string messageName);

        // Storeable
        Task<ChatMessage> OnNewChatMessage(ChatMessage message, string chatCollection);
    }
}
