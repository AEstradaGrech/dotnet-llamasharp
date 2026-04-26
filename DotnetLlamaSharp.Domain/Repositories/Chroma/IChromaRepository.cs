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

        Task<ChromaCollectionModel> GetCollection(string collection);
        Task<ChromaCollectionModel> CreateCollection(string collection);
        Task<bool> DeleteCollection(string collection);
        Task<ChromaChunk> GetChunkById(string collection, string id);
        Task<ChromaChunk> UpsertChunk(string collection, ChromaChunk chunk, bool bAddTextAsMeta = true);
        Task<bool> DeleteChunk(string collection, string id);
        /*
          CreateCollectionAsync(String, CancellationToken)	
            Creates Chroma collection.

            DeleteCollectionAsync(String, CancellationToken)	
            Removes collection by name.

            DeleteEmbeddingsAsync(String, String[], CancellationToken)	
            Removes embeddings from specified collection.

            GetCollectionAsync(String, CancellationToken)	
            Returns collection model instance by name.

            GetEmbeddingsAsync(String, String[], String[], CancellationToken)	
            Returns embeddings from specified collection.

            ListCollectionsAsync(CancellationToken)	
            Returns all collection names.

            QueryEmbeddingsAsync(String, ReadOnlyMemory<Single>[], Int32, String[], CancellationToken)	
            Searches nearest embeddings by distance in specified collection.

            UpsertEmbeddingsAsync(String, String[], ReadOnlyMemory<Single>[], Object[], CancellationToken)	
            Upserts embedding to specified collection.
         */
    }
}
