using Dotnet.Chroma.Repositories;
using Dotnet.Chroma.Repositories.Interfaces;
using Dotnet.Chroma.Repositories.Models.Metadata;
using Dotnet.Chroma.Repositories.Models.Settings;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{
    public class ChromaSysChunksRepository : ChromaRepository<SysChunksCollection, ChromaSysChunk>, IChromaSysChunksRepository
    {
        public ChromaSysChunksRepository(ILogger<ChromaRepository<SysChunksCollection, ChromaSysChunk>> logger, IOptions<ChromaSettings> dbSettings, IChromaDbClient dbClient) : base(dbClient, dbSettings)  { }

        public override async Task<IEnumerable<string>> GetDbCollections()
        {
            var collections = await CollectionsOf((int)EChunkType.SYSTEM);

            return collections.Count > 0 ? collections.Select(c => c.Name) : Enumerable.Empty<string>();
        }

        public async Task<SysChunksCollection> CreateCollection(string name, string? description, string? embeddingModel = null, int? embeddingDimensions = null)
        {
            var collectionChunk = DefaultChunk(embeddingModel ?? _settings.EmbeddingModel, embeddingDimensions ?? _settings.EmbeddingDimensions);
            
            collectionChunk.AddMetadata(nameof(ChromaCollectionMetadata.CHUNK_TYPE).ToLower(), EChunkType.SYSTEM);

            collectionChunk.Text = string.IsNullOrEmpty(description) ? $"System Messages collection" : description;

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
    }
}
