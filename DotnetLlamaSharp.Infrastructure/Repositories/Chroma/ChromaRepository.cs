
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Infrastructure.Exceptions;
using DotnetLlamaSharp.Infrastructure.Models;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Chroma;
using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using NewtonSerializer = Newtonsoft.Json;


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
        private readonly ApiSettings _settings;
        private readonly ILogger<ChromaRepository> _logger;
        public ChromaRepository(ILogger<ChromaRepository> logger, IOptions<ApiSettings> dbSettings, IChromaClient client)
        {
            _logger = logger;
            _settings = dbSettings.Value ?? throw new ArgumentNullException(nameof(ApiSettings));
            _dbClient = client ?? new ChromaClient(_settings.ServerByKey("chroma"));
        }
        public async Task<IAsyncEnumerable<string>> GetDbCollections()
            => _dbClient.ListCollectionsAsync();

        public async Task<ChromaCollection> GetCollection(string name)
        {
            var collection = await RequestCollection(name);

            if(collection != null)
            {
                var data = await getCollectionData(name);

                if (data == null)
                    throw new ArgumentNullException($"{nameof(GetCollection)} >> no collection data found ");

                return new ChromaCollection { Id = collection.Id, Name = collection.Name, Metadata = data.Metadata };
            }

            return null;
        }

        public async Task<bool> CollectionExists(string name)
        {
            await foreach(string collection in RequestCollectionList())
                if (collection == name) 
                    return true;

            return false;
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
            //if (await CollectionExists("RAGS TEST"))
            //    await DeleteCollection("RAGS TEST");

            //await RequestNewCollection("RAGS TEST");
            
            if (await CollectionExists(name))
                throw new InvalidDataException("An error has occured while creating the chroma collection: a collection with name {name} already exists");

            await RequestNewCollection(name);

            var collection = await RequestCollection(name);

            if (collection == null)
                throw new ArgumentNullException($"An error has occured while creating the chroma collection: {name}");

            if (!data.Metadata.ContainsKey("name"))
                data.Metadata.Add("name", name);

            if (!data.Metadata.ContainsKey("chunks"))
                data.Metadata.Add("chunks", 0);
            
            if (!data.Metadata.ContainsKey("description") && !string.IsNullOrEmpty(data.Text));
                data.Metadata.Add("description", data.Text);

            data.Id = "0";
            data.Embedding = null;

            // necesita COLLECTION_ID !NAME
            await RequestUpsert(collection.Id, [data.Id], new List<ReadOnlyMemory<float>>(), [data.MetadataArray]);

            var dataInsert = await getCollectionData(name);

            if (dataInsert == null)
                throw new InvalidDataException($"An error has occured while creating the collection {name}: Collection data insert is null");

            return new ChromaCollection { Id = collection.Id, Name = name, Metadata = data.Metadata };
        }

        public async Task<ChromaChunk> UpsertChunk(string collectionName, ChromaChunk chunk, bool bAddTextAsMeta = true, Dictionary<string, object> extraTags = null)
        {
            if (!await CollectionExists(collectionName))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collectionName}");

            var collection = await GetCollection(collectionName);

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

            if (extraTags != null && extraTags.Count() > 0)
                extraTags.Keys.ToList().ForEach(key => chunk.Metadata.Add(key, extraTags[key]));

            var current = await GetChunkById(collection.Id, chunk.Id);

            bool isInsert = chunk == null;
            
            var metas = chunk.MetadataArray;
                
            await RequestUpsert(collectionName, [chunk.Id], [chunk.Embedding], [chunk.MetadataArray]);
            
            var upsert = await RequestEmbeddings(collection.Id, [chunk.Id]);

            if (isInsert)
                await updateCollectionChunksCount(collectionName, bIncrease: true);

            return await GetChunkById(collectionName, chunk.Id);
        }

        public async Task<int> InsertChunks(string collectionName, List<ChromaChunk> chunks, bool bAddTextAsMeta = true, Dictionary<string, object> extraTags = null)
        {
            if (!await CollectionExists(collectionName))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collectionName}");

            var collection = await GetCollection(collectionName);

            var selectedChunks = chunks.Where(x => !x.Embedding.IsEmpty);

            if (selectedChunks.Count() == 0)
                throw new InvalidOperationException($"Invalid batch insert for collection: {collectionName} >> No embeddings to insert");
            
            var totalChunks = Convert.ToInt32(collection.Metadata["chunks"]);
            
            for (int i = 0; i<selectedChunks.Count(); i++)
            {    
                var chunk = chunks[i];

                chunk.Id = $"{totalChunks + i}";
               
                if (bAddTextAsMeta)
                {
                    if (string.IsNullOrEmpty(chunk.Text))
                        throw new InvalidOperationException("Chroma chunk has no associated text");

                    chunk.Metadata.Add("text", chunk.Text);
                }

                if (extraTags != null && extraTags.Count() > 0)
                    extraTags.Keys.ToList().ForEach(key => chunk.Metadata.Add(key, extraTags[key]));
            }

            await RequestUpsert(collection.Id, selectedChunks.Select(x => $"{x.Id}").ToList(), selectedChunks.Select(x => x.Embedding).ToList(), selectedChunks.Select(x => x.MetadataArray).ToList());

            await updateCollectionChunksCount(collectionName, bIncrease: true, amount: selectedChunks.Count());

            return selectedChunks.Count();
        }

        public async Task<ChromaChunk> GetChunkById(string collectionName, string id)
        {
            if (!await CollectionExists(collectionName))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collectionName}");

            var collection = await RequestCollection(collectionName);

            var chunk = await RequestEmbeddings(collectionName, [id]);

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
            if(await CollectionExists(collection))
            {
                await _dbClient.DeleteCollectionAsync(collection);

                return await CollectionExists(collection);
            }

            return false;
        }

        public async Task<bool> DeleteChunk(string collectionName, string id)
        {
            if (!await CollectionExists(collectionName))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collectionName}");

            var collection = await _dbClient.GetCollectionAsync(collectionName);

            await _dbClient.DeleteEmbeddingsAsync(collection.Id, [id]);

            var chunk = await GetChunkById(collection.Id, id);
            
            bool bSucceeded = chunk == null;

            if (bSucceeded)
                await updateCollectionChunksCount(collection.Id, bIncrease: false);

            return bSucceeded;
        }

        public async Task<ChromaCollection> UpdateCollectionData(string name, Dictionary<string, object> metadata)
        {
            if (!await CollectionExists(name))
                throw new ArgumentNullException($"No collection found with name: {name}");

            var data = await getCollectionData(name);

            metadata.Keys.ToList().ForEach(key => data.Metadata.Add(key, metadata[key]));

            await UpsertChunk(name, data, bAddTextAsMeta: false);

            return await GetCollection(name);
        }

        private async Task<int> updateCollectionChunksCount(string name, bool bIncrease, int amount = 1)
        {
            var collection = await GetCollection(name);

            if (collection == null)
                throw new ArgumentNullException($"An error has occured while updating the collection chunks count >> no collection found with name: {name}");

            var collectionChunk = await getCollectionData(name);

            if (amount == 0)
                amount = 1;

            int chunks = Convert.ToInt32(collectionChunk.Metadata["chunks"]) + (bIncrease ? Math.Abs(amount) : -Math.Abs(amount));

            collectionChunk.Metadata["chunks"] = chunks;

            await _dbClient.UpsertEmbeddingsAsync(collection.Id, ["0"], [], collectionChunk.MetadataArray);

            var update = await getCollectionData(name);

            return Convert.ToInt32(update.Metadata["chunks"]);
        }

        private async Task<ChromaChunk> getCollectionData(string collection)
            => await GetChunkById(collection, "0");

        public IAsyncEnumerable<string> RequestCollectionList()
        {
            try
            {
                return _dbClient.ListCollectionsAsync();
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{error.Error}");
            }
        }

        public async Task<ChromaCollectionModel> RequestCollection(string name)
        {
            try
            {
                return await _dbClient.GetCollectionAsync(name);
            }
            catch(HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{error.Error}");
            }
        }

        public async Task<ChromaCollectionModel> RequestNewCollection(string name)
        {
            try
            {
                await _dbClient.CreateCollectionAsync(name);

                return await RequestCollection(name);
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{error.Error}");
            }
        }

        public async Task RequestCollectionDelete(string name)
        {
            try
            {
                await _dbClient.DeleteCollectionAsync(name);
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{error.Error}");
            }
        }

        public async Task<ChromaEmbeddingsModel> RequestEmbeddings(string collectionId, List<string> ids, List<string> metadatas = null)
        {
            try
            {
                return await _dbClient.GetEmbeddingsAsync(collectionId, ids.ToArray(), metadatas != null ? metadatas.ToArray() : []);
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{error.Error}");
            }
        }

        public async Task RequestUpsert(string collectionId, List<string> ids, List<ReadOnlyMemory<float>> embeddings, List<object[]> metadatas = null)
        {
            try
            {
                await _dbClient.UpsertEmbeddingsAsync(collectionId, ids.ToArray(), embeddings.ToArray(), metadatas != null ? metadatas.ToArray() : []);
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{error.Error}");
            }
        }

        public async Task RequestDelete(string collectionId, List<string> ids)
        {
            try
            {
                await _dbClient.DeleteEmbeddingsAsync(collectionId, ids.ToArray());
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{error.Error}");
            }
        }

        private ChromaError handleChromaClientError(HttpOperationException ex)
            => !string.IsNullOrEmpty(ex.ResponseContent) ? JsonSerializer.Deserialize<ChromaError>(ex.ResponseContent) : new ChromaError { Error = ex.Message, Code = HttpStatusCode.InternalServerError };
    }
}
