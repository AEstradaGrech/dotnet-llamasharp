
using DocumentFormat.OpenXml.Wordprocessing;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Chroma;
using System.Collections;

#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{
    // IMPORTANT:
    //  - For this package: Microsoft.SemanticKernel.Connectors.Chroma --version 1.74.0-alpha
    //  - USE THIS exact Chroma image VERSION: chromadb/chroma:0.4.24
    //  - Run the container: docker run --name local-chroma -v ./chroma-data:/data -p XXXX:8000 chromadb/chroma:0.4.24
    public class ChromaRepository : IChromaRepository
    {
        private readonly IChromaClient _dbClient;
        private readonly ChromaDbSettings _dbSettings;
        private readonly ILogger<ChromaRepository> _logger;
        public ChromaRepository(ILogger<ChromaRepository> logger, IOptions<ChromaDbSettings> dbSettings, IChromaClient client)
        {
            _logger = logger;
            _dbSettings = dbSettings.Value ?? throw new ArgumentNullException(nameof(ChromaDbSettings));
            _dbClient = client ?? new ChromaClient(_dbSettings.ServerUrl);
        }
        public async Task<IAsyncEnumerable<string>> GetDbCollections()
            => _dbClient.ListCollectionsAsync();

        public async Task<ChromaCollection> GetCollection(string name)
        {
            var collection = await _dbClient.GetCollectionAsync(name);

            if(collection != null)
            {
                var data = await GetChunkById(name, "0");

                if (data == null)
                    throw new ArgumentNullException($"{nameof(GetCollection)} >> no collection data found ");

                return new ChromaCollection { Id = collection.Id, Name = collection.Name, Metadata = data.Metadata };
            }

            return null;
        }

        public async Task<bool> CollectionExists(string name)
        {
            var collection = await _dbClient.GetCollectionAsync(name);

            return collection != null;
        }

        public async Task<ChromaCollection> CreateCollection(string name, string description)
        {
            var metadata = new Dictionary<string, object>
            {
                { "name", name },
                { "description", description },
                { "chunks", 0 }
            };

            return await CreateCollection(name, new ChromaChunk { Id = "0", Text = description, Metadata = metadata });
        }

        public async Task<ChromaCollection> CreateCollection(string name, ChromaChunk data)
        {
            if(await CollectionExists(name))
                throw new InvalidDataException("An error has occured while creating the chroma collection: a collection with name {name} already exists");

            await _dbClient.CreateCollectionAsync(name);

            var collection = await _dbClient.GetCollectionAsync(name);

            if (collection == null)
                throw new ArgumentNullException($"An error has occured while creating the chroma collection: {name}");

            if (!data.Metadata.ContainsKey("name"))
                data.Metadata.Add("name", name);

            if (!data.Metadata.ContainsKey("chunks"))
                data.Metadata.Add("chunks", 0);
            
            if (!string.IsNullOrEmpty(data.Text) && !data.Metadata.ContainsKey("description"));
                data.Metadata.Add("description", data.Text);

            data.Id = "0";
            data.Embedding = null;

            await _dbClient.UpsertEmbeddingsAsync(name, [data.Id], [], data.MetadataArray);

            var dataInsert = await getCollectionData(name);

            if (dataInsert == null)
                throw new InvalidDataException($"An error has occured while creating the collection {name}: Collection data insert is null");

            return new ChromaCollection { Id = collection.Id, Name = name, Metadata = data.Metadata };
        }

        public async Task<ChromaChunk> UpsertChunk(string collection, ChromaChunk chunk, bool bAddTextAsMeta = true)
        {
            if (!await CollectionExists(collection))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collection}");

            if (string.IsNullOrEmpty(chunk.Id))
                throw new InvalidOperationException("Chroma chunk has no ID");
            
            if (chunk.Embedding.IsEmpty)
                throw new InvalidOperationException("Chroma chunk has no embedding");

            if (bAddTextAsMeta)
            {
                if (string.IsNullOrEmpty(chunk.Text))
                    throw new InvalidOperationException("Chroma chunk has no associated text");

                chunk.Metadata.Add("text", chunk.Text);
            }

            var current = await GetChunkById(collection, chunk.Id);

            bool isInsert = chunk == null;
            
            var metas = chunk.MetadataArray;
                
            await _dbClient.UpsertEmbeddingsAsync(collection, [chunk.Id], [chunk.Embedding], metas);
            
            var upsert = await _dbClient.GetEmbeddingsAsync(collection, [chunk.Id]);

            if (isInsert)
                await updateCollectionChunksCount(collection, bIncrease: true);

            return await GetChunkById(collection, chunk.Id);
        }

        public async Task<ChromaChunk> GetChunkById(string collection, string id)
        {
            var chunk = await _dbClient.GetEmbeddingsAsync(collection, [id]);

            if (!chunk.Ids.Any()) return null;

            var metas = chunk.Metadatas.First();

            var chunkText = "";
            if(metas.ContainsKey("text"))
            {
                chunkText = (string)metas["text"];
                metas.Remove("text");
            }
            return new ChromaChunk { Id = chunk.Ids.First(), Text = chunkText, Embedding = chunk.Embeddings.First(), Metadata = metas };
        }

        public async Task<bool> DeleteCollection(string collection)
        {
            await _dbClient.DeleteCollectionAsync(collection);

            var collections = await _dbClient.GetCollectionAsync(collection);
            
            return collection == null;
        }

        public async Task<bool> DeleteChunk(string collection, string id)
        {
            await _dbClient.DeleteEmbeddingsAsync(collection, [id]);

            var chunk = await GetChunkById(collection, id);
            
            bool bSucceeded = chunk == null;

            if (bSucceeded)
                await updateCollectionChunksCount(collection, bIncrease: false);

            return bSucceeded;
        }

        private async Task<int> updateCollectionChunksCount(string name, bool bIncrease)
        {
            var collectionChunk = await getCollectionData(name);

            int chunks = Convert.ToInt32(collectionChunk.Metadata["chunks"]) + (bIncrease ? 1 : -1);

            collectionChunk.Metadata["chunks"] = chunks;

            await _dbClient.UpsertEmbeddingsAsync(name, ["0"], [], collectionChunk.MetadataArray);

            var update = await GetChunkById(name, "0");

            return Convert.ToInt32(update.Metadata["chunks"]);
        }

        private async Task<ChromaChunk> getCollectionData(string collection)
            => await GetChunkById(collection, "0");
    }
}
