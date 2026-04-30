using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Request;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Services.Embeddings
{
    public interface IChromaService
    {
        Task<IAsyncEnumerable<string>> GetDbCollections();
        Task<ChromaCollection> CreateCollection(CreateCollectionRequest request);
        Task<bool> DeleteCollection(string name);
        Task<ChromaCollection> GetCollection(string name);
        Task<ChromaCollection> CreateCollectionFromFile(EmbedCollectionRequest request);
        Task<ChunksCollection> InspectCollection(string name, int startIndex = 0, int samples = 0, bool includeEmbeddings = false); // Default: all chunks from idx 0
        Task<ChromaQuery> QueryCollection(string name, string query, int resultsNumber, Dictionary<string, object> metadataFilters);
    }
}
