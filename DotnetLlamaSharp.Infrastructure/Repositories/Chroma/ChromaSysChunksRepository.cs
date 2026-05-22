using Dotnet.Chroma.Repositories;
using Dotnet.Chroma.Repositories.Models;
using Dotnet.Chroma.Repositories.Models.Metadata;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Chroma;


namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{
    #pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class ChromaSysChunksRepository : ChromaRepository<SysChunksCollection, ChromaSysChunk>, IChromaSysChunksRepository
    {
        public ChromaSysChunksRepository(ILogger<ChromaRepository<SysChunksCollection, ChromaSysChunk>> logger, IOptions<ChromaSettings> dbSettings, IChromaClient client) : base(client, dbSettings)  { }

        public async Task<SysChunksCollection> CreateCollection(string name, ReadOnlyMemory<float> embedding, string? description)
        {
            if(embedding.Length == 0)
                throw new InvalidOperationException($"Collection embedding length is 0. Embedding is required to perform collection queries (chroma requires an embedding despite the metadata)");

            var collectionChunk = DefaultChunk();
            
            collectionChunk.AddMetadata(nameof(ChromaCollectionMetadata.CHUNK_TYPE).ToLower(), EChunkType.SYSTEM);

            return await CreateCollection(name, collectionChunk);
        }

        public async Task<ChromaSysChunk> DeleteByName(string collectionName, string name, string? version = null)
        {
            var collection = await GetCollection(collectionName);

            var query = await GetChunks(collection.Name, new Dictionary<string, object> { [nameof(ChromaMetadata.DOCUMENT_NAME).ToLower()] = name }, withEmbeddings: false);

            if (!query.Any())
                throw new FileNotFoundException($"{collection.Name} >> {nameof(DeleteByName)} >> No system chunk has been found with name: {name}");

            if(string.IsNullOrEmpty(version))
            {
                foreach(var chunk in query)
                    if (!await DeleteChunk(collection.Name, chunk.Id))
                        throw new InvalidOperationException($"{collection.Name} >> {nameof(DeleteByName)} >> An error has occured while deleting sys chunk with name: {name} - {version}");

                return query.First();
            }
            else
            {
                if (!query.Any(chunk => chunk.GetMeta<SysChunkMetadata>().VERSION == version))
                    throw new FileNotFoundException($"{collection.Name} >> {nameof(DeleteByName)} >> No VERSION {version} has been found for sys chunk: {name}");

                var chunk = query.Where(chunk => chunk.GetMeta<SysChunkMetadata>().VERSION == version).FirstOrDefault();

                if (!await DeleteChunk(collection.Name, chunk.Id))
                    throw new InvalidOperationException($"{collection.Name} >> {nameof(DeleteByName)} >> An error has occured while deleting sys chunk with name: {name} - {version}");
                
                return chunk;
            }
            
        }

        public async Task<bool> ExistsMessage(string collectionName, string name, string? version)
        {
            var filters = new Dictionary<string, object> { [nameof(ChromaMetadata.DOCUMENT_NAME).ToLower()] = name };

            if(!string.IsNullOrEmpty(version))
                filters.Add(nameof(SysChunkMetadata.VERSION).ToLower(), version);

            var query = await GetChunks(collectionName, filters, withEmbeddings: false);

            return query.Any();
        }

        public async Task<ChromaSysChunk> GetByName(string collectionName, string name, string? version = null) //WHERE chunk.Meta.NAME = name
        {
            if (string.IsNullOrEmpty(version))
            {
                var query = await GetChunks(collectionName, new Dictionary<string, object> { [nameof(ChromaMetadata.DOCUMENT_NAME).ToLower()] = name }, withEmbeddings: false);

                if (!query.Any())
                    throw new FileNotFoundException($"{collectionName} >> {nameof(GetByName)} >> No chunks found with name: {name}");

                return query.OrderByDescending(chunk => int.Parse(chunk.Id)).First();
            }
            else
            {
                var filters = new Dictionary<string, object> { [nameof(ChromaMetadata.DOCUMENT_NAME).ToLower()] = name };
                
                filters.Add(nameof(SysChunkMetadata.VERSION).ToLower(), version);

                var query = await GetChunks(collectionName, filters, withEmbeddings: false);

                if (!query.Any())
                    throw new FileNotFoundException($"{collectionName} >> {nameof(GetByName)} >> No chunks found with name: {name}-{version}");

                return query.First();
            }
        }

        public async Task<string> GetSystemMessage(string collectionName, string name)
        {
            var message = await GetByName(collectionName, name);

            return message.Text;
        }

        public Task<ChromaSysChunk> UpdateMessage(string collectionName, string name, string message, ReadOnlyMemory<float> embedding)
        {
            throw new NotImplementedException();
        }
    }
}
