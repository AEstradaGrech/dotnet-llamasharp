
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

        public async Task<ChromaCollectionModel> GetCollection(string collectionName)
            => await _dbClient.GetCollectionAsync(collectionName);

        public async Task<ChromaCollectionModel> CreateCollection(string collectionName)
        {
            try
            {
                var collection = await _dbClient.GetCollectionAsync(collectionName);

                if (collection != null)
                    throw new InvalidOperationException($"Collection {collectionName} already exists");

                await _dbClient.CreateCollectionAsync(collectionName);
                
                return await GetCollection(collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ChromaRepository)} >> {nameof(CreateCollection)} >> {ex.Message}");
                throw new InvalidOperationException($"An error has occured while creating the ChromaDB collection: {collectionName}");
            }
        }

        public async Task<ChromaChunk> UpsertChunk(string collection, ChromaChunk chunk, bool bAddTextAsMeta = true)
        {
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

            var metas = chunk.Metadata.Select(x => new { x.Key, x.Value }).ToArray();
                await _dbClient.UpsertEmbeddingsAsync(collection, [chunk.Id], [chunk.Embedding], metas);
            
            var upsert = await _dbClient.GetEmbeddingsAsync(collection, [chunk.Id]);

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
            
            return chunk == null;
        }
    }
}
