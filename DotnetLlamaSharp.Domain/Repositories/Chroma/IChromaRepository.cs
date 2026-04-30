using System;
using System.Collections.Generic;
using System.Text;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using Microsoft.SemanticKernel.Connectors.Chroma;
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace DotnetLlamaSharp.Domain.Repositories.Chroma
{
    public interface IChromaRepository
    {
        Task<IAsyncEnumerable<string>> GetDbCollections();
        Task<bool> CollectionExists(string name);
        Task<ChunksCollection> InspectCollection(string name, List<string> chunkIds, bool includeEmbeddings = false);
        Task<ChromaCollection> GetCollection(string collection);
        Task<ChromaCollection> CreateCollection(string collection, string description);
        Task<ChromaCollection> CreateCollection(string name, ChromaChunk data);
        Task<ChromaCollection> UpdateCollectionData(string name, Dictionary<string, object> metadata);
        Task<bool> DeleteCollection(string collection);
        Task<ChromaChunk> GetChunkById(string collection, string id);
        Task<List<ChromaChunk>> GetChunks(string collection, List<string> chunkIds, bool withEmbeddings = true);
        Task<ChromaChunk> UpsertChunk(string collection, ChromaChunk chunk, bool bAddTextAsMeta = true, Dictionary<string, object> extraTags = null);
        Task<int> InsertChunks(string collection, List<ChromaChunk> chunks, bool bAddTextAsMeta = true, Dictionary<string, object> extraMetas = null);
        Task<bool> DeleteChunk(string collection, string id);

        //_client methods:
        IAsyncEnumerable<string> RequestCollectionList();
        Task<ChromaCollectionModel> RequestCollection(string name);
        Task<ChromaCollectionModel> RequestNewCollection(string name);
        Task RequestCollectionDelete(string name);
        Task<ChromaEmbeddingsModel> RequestEmbeddings(string collectionId, List<string> ids, List<string> metadatas = null);
        Task RequestUpsert(string collectionId, List<string> ids, List<ReadOnlyMemory<float>> embeddings, List<Dictionary<string, object>> metadatas = null);
        Task RequestDelete(string collectionId, List<string> ids);
    }
}
