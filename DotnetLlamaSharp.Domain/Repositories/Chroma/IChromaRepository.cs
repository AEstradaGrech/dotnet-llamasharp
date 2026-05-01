using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using Microsoft.SemanticKernel.Connectors.Chroma;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Repositories.Chroma
{
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public interface IChromaRepository<TCol, TChunk> where TCol : ChromaChunksCollection<TChunk> where TChunk : ChromaChunk
    {
        Task<IAsyncEnumerable<string>> GetDbCollections();
        Task<bool> CollectionExists(string name);
        Task<TCol> InspectCollection(string name, List<string> chunkIds, bool includeEmbeddings = false);
        Task<TCol> GetCollection(string collection);
        Task<TCol> CreateCollection(string collection, string description, string model, int dimensions);
        Task<TCol> CreateCollection(string name, ChromaChunk data);
        Task<TCol> UpdateCollectionData(string name, Dictionary<string, object> metadata, bool isOverride = true);
        Task<bool> DeleteCollection(string collection);
        Task<TChunk> GetChunkById(string collection, string id);
        Task<List<TChunk>> GetChunks(string collection, List<string> chunkIds, bool withEmbeddings = true);
        Task<TChunk> UpsertChunk(string collection, ChromaChunk chunk, bool bAddTextAsMeta = true, Dictionary<string, object> extraTags = null);
        Task<int> InsertChunks(string collection, List<ChromaChunk> chunks, bool bAddTextAsMeta = true, Dictionary<string, object> extraMetas = null);
        Task<bool> DeleteChunk(string collection, string id);
        Task<List<ChromaQueryChunk>> QueryCollection(string collection, ReadOnlyMemory<float> queryEmbedding, int resultsNumber, Dictionary<string, object> filters = null);
        //_client methods:
        IAsyncEnumerable<string> RequestCollectionList();
        Task<ChromaCollectionModel> RequestCollection(string name);
        Task<ChromaCollectionModel> RequestNewCollection(string name);
        Task RequestCollectionDelete(string name);
        Task<ChromaEmbeddingsModel> RequestEmbeddings(string collectionId, List<string> ids, List<string> metadatas = null);
        Task RequestUpsert(string collectionId, List<string> ids, List<ReadOnlyMemory<float>> embeddings, List<Dictionary<string, object>> metadatas = null);
        Task RequestDelete(string collectionId, List<string> ids);
        Task<ChromaQueryResultModel> RequestQuery(string collectionId, ReadOnlyMemory<float>[] queryEmbeddings, int nResults);
        Task<ChromaQueryResultModel> RequestFilterQuery(string collectionId, ReadOnlyMemory<float> queryEmbeddings, int nResults, Dictionary<string, object> filters);
    }
}
