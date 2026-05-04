using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Chroma;


namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{
    #pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class ChromaSysChunksRepository : ChromaRepository<SysChunksCollection, ChromaSysChunk>, IChromaSysChunksRepository
    {
        public ChromaSysChunksRepository(ILogger<ChromaRepository<SysChunksCollection, ChromaSysChunk>> logger, IOptions<ApiSettings> dbSettings, IChromaClient client) : base(logger, dbSettings, client) 
        {
            
        }

        public async Task<SysChunksCollection> CreateCollection(string name, ReadOnlyMemory<float> embedding, string? description)
        {
            if(embedding.Length == 0)
                throw new InvalidOperationException($"Collection embedding length is 0. Embedding is required to perform collection queries (chroma requires an embedding despite the metadata)");

            var collectionChunk = DefaultChunk();

            collectionChunk.AddMetadata(nameof(SysCollectionMetadata.COL_EMBEDDING).ToLower(), embedding);
            collectionChunk.AddMetadata(nameof(SysCollectionMetadata.TEXT).ToLower(), string.IsNullOrEmpty(description) ? name : description, resetDefault: true);

            return await CreateCollection(name, collectionChunk);
        }

        public async Task<ChromaSysChunk> GetByName(string collectionName, string name)
        {
            var collection = await GetCollection(collectionName);

            var results = await QueryCollection(collectionName, collection.CollectionEmbedding, 1, new Dictionary<string, object> { [nameof(ChromaSysChunk.Name).ToLower()] = name });

            if (!results.Any())
                throw new ArgumentNullException($"{collection.Name} >> {nameof(GetByName)} >> No System Message Chunk has been found with name {name}");

            return results.First().As<ChromaSysChunk>();
        }
    }
}
