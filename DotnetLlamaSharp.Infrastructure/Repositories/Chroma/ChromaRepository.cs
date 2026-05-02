using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Infrastructure.Exceptions;
using DotnetLlamaSharp.Infrastructure.Extensions.Chroma;
using DotnetLlamaSharp.Infrastructure.Models;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Chroma;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;

namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{

#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public abstract class  ChromaRepository<TCol, TChunk> where TCol : ChromaChunksCollection<TChunk> where TChunk : ChromaChunk
    {
        private readonly IChromaClient _dbClient;
        protected readonly ApiSettings _settings;
        protected readonly ILogger<ChromaRepository<TCol, TChunk>> _logger;
        public ChromaRepository(ILogger<ChromaRepository<TCol, TChunk>> logger, IOptions<ApiSettings> dbSettings, IChromaClient client)
        {
            _logger = logger;
            _settings = dbSettings.Value ?? throw new ArgumentNullException(nameof(ApiSettings));
            _dbClient = client ?? new ChromaClient(_settings.EndpointByKey("chroma"));
        }
        public async Task<IAsyncEnumerable<string>> GetDbCollections()
            => _dbClient.ListCollectionsAsync();

        public async Task<TCol> GetCollection(string name)
        {
            var collection = await RequestCollection(name);

            if (collection != null)
            {
                var data = await GetChunkById(name, "0");

                if (data == null)
                    throw new ArgumentNullException($"No data has been found for collection: {name}");

                return Activator.CreateInstance(typeof(TCol), collection.Id, collection.Name, data.Metadata) as TCol;
            }
            return null;
        }

        public async Task<bool> CollectionExists(string name)
        {
            await foreach (string collection in RequestCollectionList())
                if (collection == name)
                    return true;

            return false;
        }

        public async Task<TCol> CreateCollection(string name, string description, string? model = null, int? dimensions = null)
        {
            if (string.IsNullOrEmpty(model))
                model = _settings.DefaultEmbedder;

            var metadata = getDefaultCollectionMetadata(description, model, dimensions);

            return await CreateCollection(name, new ChromaChunk("0", "", metadata));
        }

        public async Task<TCol> CreateCollection(string name, ChromaChunk data)
        {
            if (await CollectionExists(name))
                throw new InvalidDataException($"An error has occured while creating the chroma collection: a collection with name {name} already exists");

            await RequestNewCollection(name);

            var collection = await RequestCollection(name);

            if (collection == null)
                throw new ArgumentNullException($"An error has occured while creating the chroma collection: {name}");

            // Default with name = description = TEXT & chunkOptions | apiDefaults
            var defaultMetadata = getDefaultCollectionMetadata(name, data.DefaultMetadata.MODEL ?? null, data.DefaultMetadata.DIMENSIONS);

            //if chunk has already some of the default values, preserve them. Do not reserialize the DefaultMeta to validate next step (TEXT)
            defaultMetadata.Keys.ToList().ForEach(key => data.AddMetadata(key, defaultMetadata[key], resetDefault: false, isOverride: false));

            //if chunk has some TEXT, then it is the desired CollectionDescription, so default TEXT meta is overrided
            if (!string.IsNullOrEmpty(data.Text))
                data.AddMetadata(nameof(ChromaCollectionMetadata.TEXT).ToLower(), data.Text);

            data.Id = "0";
            data.Embedding = null;

            await RequestUpsert(collection.Id, [data.Id], [], [data.Metadata]);

            var dataInsert = await GetCollection(name);

            if (dataInsert == null)
                throw new InvalidDataException($"An error has occured while creating the collection {name}: Collection data insert is null");

            return Activator.CreateInstance(typeof(TCol), collection.Id, name, data.Metadata) as TCol;
        }

        public async Task<List<ChromaQueryChunk>> QueryCollection(string name, ReadOnlyMemory<float> queryEmbedding, int resultsNumber, Dictionary<string, object> filters = null)
        {
            var collection = await GetCollection(name);

            var queryResult = filters == null ?
                await RequestQuery(collection.Id, [queryEmbedding], resultsNumber) :
                await RequestFilterQuery(collection.Id, queryEmbedding, resultsNumber, filters);

            var results = new List<ChromaQueryChunk>();

            if (queryResult.Ids.Any() && queryResult.Distances.Any() && queryResult.Metadatas.Any())
            {
                var resultIds = queryResult.Ids.FirstOrDefault();
                var resultDistances = queryResult.Distances.FirstOrDefault();
                var resultMetas = queryResult.Metadatas.First();

                if (resultIds.Count != resultsNumber || resultIds.Count != resultsNumber || resultMetas.Count != resultsNumber)
                    throw new InvalidDataException($"Invalid number of results");

                for (int i = 0; i < resultIds.Count; i++)
                    results.Add(new ChromaQueryChunk(resultIds[i], resultDistances[i], resultMetas[i]));
            }

            return results;
        }

        public async Task<TChunk> UpsertChunk(string collectionName, ChromaChunk chunk, bool bAddTextAsMeta = true, Dictionary<string, object> extraTags = null)
        {
            if (!await CollectionExists(collectionName))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collectionName}");

            var collection = await GetCollection(collectionName);

            if (!string.IsNullOrEmpty(chunk.Id))
            {
                if (bAddTextAsMeta)
                {
                    if (string.IsNullOrEmpty(chunk.Text))
                        throw new InvalidOperationException("Chroma chunk has no associated text");

                    chunk.AddMetadata(nameof(FileChunkMetadata.TEXT).ToLower(), chunk.Text);
                }

                if (extraTags != null && extraTags.Count() > 0)
                    extraTags.Keys.ToList().ForEach(key => chunk.AddMetadata(key, extraTags[key], resetDefault: true));

                await RequestUpsert(collection.Id, [chunk.Id], [chunk.Embedding], [chunk.Metadata]);

                return await GetChunkById(collectionName, chunk.Id);
            }

            else return await InsertChunk(collectionName, chunk);
        }

        public async Task<TChunk> InsertChunk(string collectionName, ChromaChunk chunk, bool bAddTextAsMeta = true, Dictionary<string, object> extraMetas = null)
        {
            if (!await CollectionExists(collectionName))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collectionName}");

            var collection = await GetCollection(collectionName);

            if (bAddTextAsMeta)
            {
                if (string.IsNullOrEmpty(chunk.Text))
                    throw new InvalidOperationException("Chroma chunk has no associated text");

                chunk.AddMetadata(nameof(ChromaMetadata.TEXT).ToLower(), chunk.Text);
            }

            if (extraMetas != null && extraMetas.Count() > 0)
                extraMetas.Keys.ToList().ForEach(key => chunk.AddMetadata(key, extraMetas[key]));

            chunk.Id = $"{collection.GetMeta<ChromaCollectionMetadata>().CHUNKS + 1}";

            await RequestUpsert(collection.Id, [chunk.Id], [chunk.Embedding], [chunk.Metadata]);

            await updateCollectionChunksCount(collectionName, bIncrease: true);

            return await GetChunkById(collection.Name, chunk.Id);
        }
        
        public async Task<int> InsertChunks(string collectionName, List<ChromaChunk> chunks, bool bAddTextAsMeta = true, Dictionary<string, object> extraTags = null)
        {
            if (!await CollectionExists(collectionName))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collectionName}");

            var collection = await GetCollection(collectionName);

            var selectedChunks = chunks.Where(x => !x.Embedding.IsEmpty);

            if (selectedChunks.Count() == 0)
                throw new InvalidOperationException($"Invalid batch insert for collection: {collectionName} >> No embeddings to insert");

            var totalChunks = collection.GetMeta<ChromaCollectionMetadata>().CHUNKS;

            for (int i = 0; i < selectedChunks.Count(); i++)
            {
                var chunk = chunks[i];

                chunk.Id = $"{totalChunks + (i + 1)}";

                if (bAddTextAsMeta)
                {
                    if (string.IsNullOrEmpty(chunk.Text))
                        throw new InvalidOperationException("Chroma chunk has no associated text");

                    chunk.AddMetadata(nameof(ChromaMetadata.TEXT).ToLower(), chunk.Text);
                }

                if (extraTags != null && extraTags.Count() > 0)
                    extraTags.Keys.ToList().ForEach(key => chunk.AddMetadata(key, extraTags[key]));
            }

            await RequestUpsert(collection.Id, selectedChunks.Select(x => $"{x.Id}").ToList(), selectedChunks.Select(x => x.Embedding).ToList(), selectedChunks.Select(x => x.Metadata).ToList());

            await updateCollectionChunksCount(collectionName, bIncrease: true, amount: selectedChunks.Count());

            return selectedChunks.Count();
        }

        public async Task<TCol> InspectCollection(string name, List<string> chunkIds, bool includeEmbeddings = false)
        {
            var collection = await GetCollection(name);

            var chunks = await GetChunks(name, chunkIds, includeEmbeddings);

            return Activator.CreateInstance(typeof(TCol), collection.Id, collection.Name, collection.Metadata, chunks) as TCol;
        }

        public async Task<List<TChunk>> GetChunks(string collectionName, List<string> chunkIds, bool withEmbeddings = true)
        {
            var collection = await GetCollection(collectionName);

            List<string> metadatas = ["documents", "metadatas"];

            if (withEmbeddings)
                metadatas.Add("embeddings");

            var chunks = await RequestEmbeddings(collection.Id, chunkIds, metadatas);

            var debugJson = JsonSerializer.Serialize(chunks);

            var results = new List<TChunk>();

            for (int i = 0; i < chunks.Ids.Count; i++)
                results.Add((TChunk)Activator.CreateInstance(typeof(TChunk), chunks.Ids[i], withEmbeddings ? i <= chunks.Embeddings.Count ? new ReadOnlyMemory<float>(chunks.Embeddings[i]) : new ReadOnlyMemory<float>([]) : new ReadOnlyMemory<float>([]), i <= chunks.Metadatas.Count ? chunks.Metadatas[i] : []));

            return results;
        }

        public async Task<TChunk> DefaultChunk(Dictionary<string, object>? metadata = null)
        {
            var chunk = Activator.CreateInstance(typeof(TChunk)) as TChunk;

            if (metadata != null)
                chunk.CloneMetadata(metadata);

            return chunk;
        }
        public async Task<TChunk> GetChunkById(string collectionName, string id)
        {
            if (!await CollectionExists(collectionName))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collectionName}");

            var collection = await RequestCollection(collectionName);

            var chunk = await RequestEmbeddings(collection.Id, [id], ["documents", "embeddings", "metadatas"]);

            if (!chunk.Ids.Any()) return null;

            var metas = chunk.Metadatas.Any() ? chunk.Metadatas.First() : [];
            var embedding = chunk.Embeddings.Any() ? new ReadOnlyMemory<float>(chunk.Embeddings.First()) : new ReadOnlyMemory<float>([]);

            return Activator.CreateInstance(typeof(TChunk), chunk.Ids.First(), embedding, metas) as TChunk;
        }

        public async Task<bool> DeleteCollection(string collection)
        {
            if (await CollectionExists(collection))
            {
                await RequestCollectionDelete(collection);

                return !await CollectionExists(collection);
            }

            return false;
        }

        public async Task<bool> DeleteChunk(string collectionName, string id)
        {
            if (!await CollectionExists(collectionName))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collectionName}");

            var collection = await RequestCollection(collectionName);

            await RequestDelete(collection.Id, [id]);

            var chunk = await GetChunkById(collection.Id, id);

            bool bSucceeded = chunk == null;

            if (bSucceeded)
                await updateCollectionChunksCount(collection.Id, bIncrease: false);

            return bSucceeded;
        }

        public async Task<TCol> UpdateCollectionData(string name, Dictionary<string, object> metadata, bool isOverride = true)
        {
            if (!await CollectionExists(name))
                throw new ArgumentNullException($"No collection found with name: {name}");

            var data = await GetCollection(name);

            metadata.Keys.ToList().ForEach(key => data.AddMetadata(key, metadata[key], isOverride));

            await RequestUpsert(data.Id, ["0"], [], [data.Metadata]);

            return await GetCollection(name);
        }

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
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{nameof(RequestCollection)} >> {error.Error}");
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

                throw new ChromaClientException(error.Code, $"{nameof(RequestNewCollection)} >> {error.Error}");
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

                throw new ChromaClientException(error.Code, $"{nameof(RequestCollectionDelete)} >> {error.Error}");
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

                throw new ChromaClientException(error.Code, $"{nameof(RequestEmbeddings)} >> {error.Error}");
            }
        }

        public async Task RequestUpsert(string collectionId, List<string> ids, List<ReadOnlyMemory<float>> embeddings, List<Dictionary<string, object>> metadatas = null)
        {
            try
            {
                await _dbClient.UpsertEmbeddingsAsync(collectionId, ids.ToArray(), embeddings.ToArray(), metadatas != null ? metadatas.ToArray() : []);
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{nameof(RequestUpsert)} >> {error.Error}");
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

                throw new ChromaClientException(error.Code, $"{nameof(RequestDelete)} >> {error.Error}");
            }
        }

        public async Task<ChromaQueryResultModel> RequestQuery(string collectionId, ReadOnlyMemory<float>[] queryEmbeddings, int nResults)
        {
            try
            {
                return await _dbClient.QueryEmbeddingsAsync(collectionId, queryEmbeddings, nResults, ["documents", "metadatas", "distances"]);
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{nameof(RequestDelete)} >> {error.Error}");
            }
        }

        public async Task<ChromaQueryResultModel> RequestFilterQuery(string collectionId, ReadOnlyMemory<float> queryEmbeddings, int nResults, Dictionary<string, object> filters)
        {
            try
            {
                return await _dbClient.FilterQuery(_settings.EndpointByKey("chroma"), collectionId, nResults, [queryEmbeddings], filters);
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{nameof(RequestDelete)} >> {error.Error}");
            }
        }

        private ChromaClientError handleChromaClientError(HttpOperationException ex)
            => !string.IsNullOrEmpty(ex.ResponseContent) ? JsonSerializer.Deserialize<ChromaClientError>(ex.ResponseContent) : new ChromaClientError { Error = ex.Message, Code = HttpStatusCode.InternalServerError };

        private async Task<int> updateCollectionChunksCount(string name, bool bIncrease, int amount = 1)
        {
            var collection = await GetCollection(name);

            if (collection == null)
                throw new ArgumentNullException($"An error has occured while updating the collection chunks count >> no collection found with name: {name}");

            if (amount == 0)
                amount = 1;

            collection.AddMetadata(nameof(ChromaCollectionMetadata.CHUNKS).ToLower(), collection.GetMeta<ChromaCollectionMetadata>().CHUNKS + (bIncrease ? Math.Abs(amount) : -Math.Abs(amount)));

            await RequestUpsert(collection.Id, ["0"], [], [collection.Metadata]);

            var update = await GetCollection(name);

            return update.GetMeta<ChromaCollectionMetadata>().CHUNKS;
        }

        private Dictionary<string, object> getDefaultCollectionMetadata(string description, string? embeddingModel = null, int? dimensions = null)
            => new Dictionary<string, object>
            {
                { nameof(ChromaCollectionMetadata.TEXT).ToLower(), description },
                { nameof(ChromaCollectionMetadata.MODEL).ToLower(), embeddingModel ?? _settings.DefaultEmbedder },
                { nameof(ChromaCollectionMetadata.DIMENSIONS).ToLower(), dimensions <= 0 ? _settings.DefaultDimensions : dimensions },
                { nameof(ChromaCollectionMetadata.CHUNKS).ToLower(), 0 }
            };
    }
}
