using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using Microsoft.SemanticKernel.Connectors.Chroma;

namespace DotnetLlamaSharp.Domain.Repositories.Chroma
{
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public interface IChromaRepository<TCol, TChunk> where TCol : ChromaChunksCollection<TChunk> where TChunk : ChromaChunk
    {
        Task<IAsyncEnumerable<string>> GetDbCollections();
        Task<bool> CollectionExists(string name);
        Task<TCol> InspectCollection(string name, List<string> chunkIds, bool includeEmbeddings = false);
        Task<TCol> GetCollection(string collection);
        Task<TCol> CreateCollection(string collection, string description, string? model = null, int? dimensions = null);
        Task<TCol> CreateCollection(string name, ChromaChunk data);
        Task<TCol> UpdateCollectionData(string name, Dictionary<string, object> metadata, bool isOverride = true);
        Task<bool> DeleteCollection(string collection);
        TChunk DefaultChunk(Dictionary<string, object>? metadata = null);
        TChunk DefaultChunk(string embeddingModel, int dimensions);
        Task<TChunk> DefaultChunk(string collectionName);
        Task<TChunk> GetChunkById(string collection, string id);
        Task<List<TChunk>> GetChunks(string collection, List<string> chunkIds, bool withEmbeddings = true);
        Task<List<TChunk>> GetChunks(string collection, Dictionary<string, object>? filters = null, bool withEmbeddings = true, int? pageSize = null, int? skip = null);
        Task<TChunk> UpsertChunk(string collection, ChromaChunk chunk, Dictionary<string, object> extraTags = null);
        Task<int> InsertChunks(string collection, List<ChromaChunk> chunks, Dictionary<string, object> extraMetas = null);
        Task<TChunk> InsertChunk(string collection, ChromaChunk chunk, Dictionary<string, object> extraMetas = null);
        Task<bool> DeleteChunk(string collection, string id);
        Task<List<ChromaQueryChunk>> QueryCollection(string collection, ReadOnlyMemory<float> queryEmbedding, int resultsNumber, Dictionary<string, object> filters = null);
    }
}
