using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Request;


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

        Task<SysChunksCollection> CreateSystemChunksCollection(string name, string? description);
        Task<ChromaSysChunk> AddSysMessage(string collectionName, ChromaSysChunk chunk, string? version);
        Task<ChromaSysChunk> GetSysMessage(string collectionName, string name);
    }
}
