using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
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

    }
}
