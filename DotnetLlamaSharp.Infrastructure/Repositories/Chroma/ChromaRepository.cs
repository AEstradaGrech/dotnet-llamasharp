using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Infrastructure.Exceptions;
using DotnetLlamaSharp.Infrastructure.Extensions.Chroma;
using DotnetLlamaSharp.Infrastructure.Extensions.Chroma.Models;
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
    // IMPORTANT:
    //  - For this package: Microsoft.SemanticKernel.Connectors.Chroma --version 1.74.0-alpha
    //  - USE THIS exact Chroma image VERSION: chromadb/chroma:0.4.24
    //  - Run the container: docker run --name local-chroma -v ./chroma-data:/data -p XXXX:8000 chromadb/chroma:0.4.24
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

                return Activator.CreateInstance(typeof(TCol), collection.Id, collection.Name, data.Text, data.Metadata) as TCol;
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

            return await CreateCollection(name, new ChromaChunk("0", description, new ReadOnlyMemory<float>([]), metadata));
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

            data.Id = "0";
            data.Embedding = null;

            await RequestEmbeddingsUpsert(collection.Id, [data.Id], [data.Text], null, [data.Metadata], isCreate: true);

            return await GetCollection(name);
        }

        public async Task<List<ChromaQueryChunk>> QueryCollection(string name, ReadOnlyMemory<float> queryEmbedding, int resultsNumber, Dictionary<string, object> filters = null)
        {
            var collection = await GetCollection(name);

            var queryResult = await RequestQuery(name, queryEmbedding, resultsNumber, filters);

            var results = new List<ChromaQueryChunk>();

            if (queryResult.Ids.Count > 1 && queryResult.Distances.Count > 1 && queryResult.Metadatas.Count > 1 && queryResult.Documents.Count > 1 && queryResult.Embeddings.Count > 1)
            {
                var resultIds = queryResult.Ids.First();
                var resultDistances = queryResult.Distances.First();
                var resultMetas = queryResult.Metadatas.First();
                var resultDocuments = queryResult.Documents.First();
                var resultEmbeddings = queryResult.Embeddings.First();

                if (resultIds.Count != resultsNumber || resultIds.Count != resultsNumber || resultMetas.Count != resultsNumber)
                    throw new InvalidDataException($"Invalid number of results");

                for (int i = 0; i < resultIds.Count; i++)
                    results.Add(Activator.CreateInstance(typeof(ChromaQueryChunk), resultIds[i], resultDistances[i], resultDocuments[i], resultEmbeddings[i] ?? new ReadOnlyMemory<float>([]), resultMetas[i] ?? new Dictionary<string, object>()) as ChromaQueryChunk);
            }

            return results;
        }

        public async Task<TChunk> UpsertChunk(string collectionName, ChromaChunk chunk, Dictionary<string, object> extraTags = null)
        {
            if (!await CollectionExists(collectionName))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collectionName}");

            var collection = await GetCollection(collectionName);

            if (!string.IsNullOrEmpty(chunk.Id))
            {
                if (extraTags != null && extraTags.Count() > 0)
                    extraTags.Keys.ToList().ForEach(key => chunk.AddMetadata(key, extraTags[key], resetDefault: true));

                await RequestEmbeddingsUpsert(collection.Id, [chunk.Id], [chunk.Text], [chunk.Embedding], [chunk.Metadata]);

                return await GetChunkById(collectionName, chunk.Id);
            }

            else return await InsertChunk(collectionName, chunk, extraTags);
        }

        public async Task<TChunk> InsertChunk(string collectionName, ChromaChunk chunk, Dictionary<string, object> extraMetas = null)
        {
            if (!await CollectionExists(collectionName))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collectionName}");

            var collection = await GetCollection(collectionName);

            if (string.IsNullOrEmpty(chunk.Text))
                throw new InvalidOperationException("Chroma chunk has no associated document");

            if (extraMetas != null && extraMetas.Count() > 0)
                extraMetas.Keys.ToList().ForEach(key => chunk.AddMetadata(key, extraMetas[key]));

            chunk.Id = $"{collection.GetMeta<ChromaCollectionMetadata>().TOTAL_CHUNKS + 1}";

            await RequestEmbeddingsUpsert(collection.Id, [chunk.Id], [chunk.Text], [chunk.Embedding], [chunk.Metadata], isCreate: true);

            await updateCollectionChunksCount(collectionName, bIncrease: true);

            return await GetChunkById(collection.Name, chunk.Id);
        }
        
        public async Task<int> InsertChunks(string collectionName, List<ChromaChunk> chunks, Dictionary<string, object> extraTags = null)
        {
            if (!await CollectionExists(collectionName))
                throw new InvalidOperationException($"No Chroma Collection has been found with name: {collectionName}");

            var collection = await GetCollection(collectionName);

            var selectedChunks = chunks.Where(x => !x.Embedding.IsEmpty);

            if (selectedChunks.Count() == 0)
                throw new InvalidOperationException($"Invalid batch insert for collection: {collectionName} >> No embeddings to insert");

            var totalChunks = collection.GetMeta<ChromaCollectionMetadata>().TOTAL_CHUNKS;

            for (int i = 0; i < selectedChunks.Count(); i++)
            {
                var chunk = chunks[i];

                chunk.Id = $"{totalChunks + (i + 1)}";

                if (string.IsNullOrEmpty(chunk.Text))
                    throw new InvalidOperationException("Chroma chunk has no associated document");

                if (extraTags != null && extraTags.Count() > 0)
                    extraTags.Keys.ToList().ForEach(key => chunk.AddMetadata(key, extraTags[key]));
            }

            await RequestEmbeddingsUpsert(collection.Id, selectedChunks.Select(x => $"{x.Id}").ToList(), selectedChunks.Select(x => x.Text).ToList(), selectedChunks.Select(x => x.Embedding).ToList(), selectedChunks.Select(x => x.Metadata).ToList(), isCreate: true);

            await updateCollectionChunksCount(collectionName, bIncrease: true, amount: selectedChunks.Count());

            return selectedChunks.Count();
        }

        public async Task<TCol> InspectCollection(string name, List<string> chunkIds, bool includeEmbeddings = false)
        {
            var collection = await GetCollection(name);

            return Activator.CreateInstance(typeof(TCol), collection.Id, collection.Name, collection.Description, collection.Metadata, await GetChunks(name, chunkIds, includeEmbeddings)) as TCol;
        }

        public async Task<List<TChunk>> GetChunks(string collectionName, List<string> chunkIds, bool withEmbeddings = true)
        {
            var collection = await GetCollection(collectionName);

            return mapChunks(await RequestDocuments(collection.Id, chunkIds, withEmbeddings), withEmbeddings);
        }

        public async Task<List<TChunk>> GetChunks(string collectionName, Dictionary<string, object>? filters = null, bool withEmbeddings = true, int ? pageSize = null, int? skip = null)
        {
            var collection = await GetCollection(collectionName);

            return mapChunks(await RequestDocuments(collection.Id, withEmbeddings, filters, pageSize, skip), withEmbeddings);
        }

        public async Task<TChunk> DefaultChunk(string collectionName)
        {
            var collection = await GetCollection(collectionName);

            if (string.IsNullOrEmpty(collection.DefaultMetadata.MODEL))
                throw new InvalidDataException($"{nameof(DefaultChunk)} >> {collectionName} >> Invalid collection settings >> Embedding MODEL name empty");

            if (collection.DefaultMetadata.DIMENSIONS <= 0)
                throw new InvalidDataException($"{nameof(DefaultChunk)} >> {collectionName} >> Invalid collection settings >> Embedding DIMENSIONS value ({collection.DefaultMetadata.DIMENSIONS})");

            var chunk = DefaultChunk(collection.DefaultMetadata.MODEL, collection.DefaultMetadata.DIMENSIONS);

            chunk.AddMetadata(nameof(ChromaMetadata.DOCUMENT_NAME).ToLower(), collection.DefaultMetadata.DOCUMENT_NAME);

            return chunk;
        }
        public TChunk DefaultChunk(string embeddingModel, int dimensions)
        {
            if (string.IsNullOrEmpty(embeddingModel))
                embeddingModel = _settings.DefaultEmbedder;

            if (dimensions <= 0)
                dimensions = _settings.DefaultDimensions;

            var metadata = new Dictionary<string, object>();

            metadata.Add(nameof(ChromaMetadata.MODEL).ToLower(), embeddingModel);

            metadata.Add(nameof(ChromaMetadata.DIMENSIONS).ToLower(), dimensions);

            return DefaultChunk(metadata);
        }

        public TChunk DefaultChunk(Dictionary<string, object>? metadata = null)
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

            var chunks = await RequestDocuments(collection.Id, [id], withEmbeddings: true);

            return mapChunks(chunks, withEmbeddings: true).FirstOrDefault();
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

            var chunk = await GetChunkById(collection.Name, id);

            bool bSucceeded = chunk == null;

            if (bSucceeded)
                await updateCollectionChunksCount(collection.Name, bIncrease: false);

            return bSucceeded;
        }

        public async Task<TCol> UpdateCollectionData(string name, Dictionary<string, object> metadata, bool isOverride = true)
        {
            if (!await CollectionExists(name))
                throw new ArgumentNullException($"No collection found with name: {name}");

            var data = await GetCollection(name);

            metadata.Keys.ToList().ForEach(key => data.AddMetadata(key, metadata[key], isOverride));

            await RequestEmbeddingsUpsert(data.Id, ["0"], [data.Description], null, [data.Metadata]);

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

        public async Task<DocumentGetResultModel> RequestDocuments(string collectionId, List<string> ids, bool withEmbeddings = true)
        {
            try
            {
                return await _dbClient.GetDocuments(_settings.EndpointByKey("chroma"), collectionId, ids, withEmbeddings: withEmbeddings);
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{nameof(RequestDocuments)} >> {error.Error}");
            }
        }

        public async Task<DocumentGetResultModel> RequestDocuments(string collectionId, bool withEmbeddings, Dictionary<string, object>? filters = null, int? pageSize = null, int? skip = null)
        {
            try
            {
                return await _dbClient.GetDocuments(_settings.EndpointByKey("chroma"), collectionId, withEmbeddings, filters, pageSize, skip);
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{nameof(RequestDocuments)} >> {error.Error}");
            }
        }

        //null = keep current, value = override
        public async Task<DocumentGetResultModel> RequestEmbeddingsUpsert(string collectionId, List<string> ids, List<string>? texts, List<ReadOnlyMemory<float>>? embeddings, List<Dictionary<string, object>>? metadatas = null, bool isCreate = false)
        {
            try
            {
                return await _dbClient.UpsertDocuments(_settings.EndpointByKey("chroma"), collectionId, ids, texts, embeddings, metadatas, isCreate);
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{nameof(RequestEmbeddingsUpsert)} >> {error.Error}");
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

        public async Task<DocumentsQueryResultModel> RequestQuery(string collectionId, ReadOnlyMemory<float> queryEmbeddings, int nResults, Dictionary<string, object> filters)
        {
            try
            {
                return await _dbClient.QueryDocuments(_settings.EndpointByKey("chroma"), collectionId, [queryEmbeddings], nResults, filters);
            }
            catch (HttpOperationException ex)
            {
                var error = handleChromaClientError(ex);

                throw new ChromaClientException(error.Code, $"{nameof(RequestDelete)} >> {error.Error}");
            }
        }

        private ChromaClientError handleChromaClientError(HttpOperationException ex)
            => !string.IsNullOrEmpty(ex.ResponseContent) ? JsonSerializer.Deserialize<ChromaClientError>(ex.ResponseContent) : new ChromaClientError { Error = ex.Message, Code = HttpStatusCode.InternalServerError };

        private List<TChunk> mapChunks(DocumentGetResultModel query, bool withEmbeddings)
        {
            var results = new List<TChunk>();

            if (!query.Ids.Any()) return results;

            for (int i = 0; i < query.Ids.Count; i++)
                results.Add((TChunk)Activator.CreateInstance(typeof(TChunk), query.Ids[i],
                    i <= query.Documents.Count ? query.Documents[i] : string.Empty,
                    withEmbeddings ? query.Embeddings != null && query.Embeddings.Any() && i <= query.Embeddings.Count? new ReadOnlyMemory<float>(query.Embeddings[i]) : new ReadOnlyMemory<float>([]) : new ReadOnlyMemory<float>([]),
                    i <= query.Metadatas.Count ? query.Metadatas[i] : []));

            return results;
        }
        private async Task<int> updateCollectionChunksCount(string name, bool bIncrease, int amount = 1)
        {
            var collection = await GetCollection(name);

            if (collection == null)
                throw new ArgumentNullException($"An error has occured while updating the collection chunks count >> no collection found with name: {name}");

            if (amount == 0)
                amount = 1;

            collection.AddMetadata(nameof(ChromaCollectionMetadata.TOTAL_CHUNKS).ToLower(), collection.GetMeta<ChromaCollectionMetadata>().TOTAL_CHUNKS + (bIncrease ? Math.Abs(amount) : -Math.Abs(amount)));

            await RequestEmbeddingsUpsert(collection.Id, ["0"], [collection.Description], [], [collection.Metadata]);

            var update = await GetCollection(name);

            return update.GetMeta<ChromaCollectionMetadata>().TOTAL_CHUNKS;
        }

        private Dictionary<string, object> getDefaultCollectionMetadata(string description, string? embeddingModel = null, int? dimensions = null)
            => new Dictionary<string, object>
            {
                { nameof(ChromaCollectionMetadata.MODEL).ToLower(), embeddingModel ?? _settings.DefaultEmbedder },
                { nameof(ChromaCollectionMetadata.DIMENSIONS).ToLower(), dimensions <= 0 ? _settings.DefaultDimensions : dimensions },
                { nameof(ChromaCollectionMetadata.TOTAL_CHUNKS).ToLower(), 0 }
            };
    }
}
