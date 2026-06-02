using Dotnet.Chroma.Repositories.Models;
using Dotnet.Chroma.Repositories.Models.Interfaces;
using Dotnet.Chroma.Repositories.Models.Metadata;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Interfaces.Model;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Embedding;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Request.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Embeddings;

namespace DotnetLlamaSharp.Services.Embeddings
{
    public class ChromaService : IChromaService
    {
        private readonly ILogger<ChromaService> _logger;
        private readonly IChromaFilesService _fileMgmtService;
        private readonly IEmbeddingsService _embeddingsService;
        private readonly IChromaChunksRepository _repo;
        private readonly IChromaSysChunksRepository _sysRepo;
        private readonly IChromaChatsRepository _chatsRepo;
        public ChromaService(ILogger<ChromaService> logger, IChromaFilesService ingestionService, IEmbeddingsService embeddingsService, 
            IChromaChunksRepository repo, IChromaSysChunksRepository sysRepo, IChromaChatsRepository chatsRepo)
        {
            _logger = logger;
            _fileMgmtService = ingestionService;
            _embeddingsService = embeddingsService;
            _repo = repo;
            _sysRepo = sysRepo;
            _chatsRepo = chatsRepo;
        }

        public async Task<ChromaFilesCollection> CreateEmptyFileCollection(CreateCollectionRequest request)
            => await _fileMgmtService.CreateEmptyFileCollection(request);

        public async Task<ChromaFilesCollection> CreateCollectionFromFile(EmbedCollectionRequest request)
            => await _fileMgmtService.CreateCollectionFromFile(request);

        public async Task<bool> DeleteCollection(string name)
            => await _repo.DeleteCollection(name);

        public async Task<ChromaChunksCollection<ChromaChunk>> GetCollection(string name)
            => await _repo.GetCollection(name);

        public Task<IAsyncEnumerable<string>> GetDbCollections()
            => _repo.GetDbCollections();

        public async Task<ChromaFilesCollection> InspectFilesCollection(string name, int startIndex = 0, int samples = 0, bool includeEmbeddings = false)
            => await _fileMgmtService.InspectCollection(name, startIndex, samples, includeEmbeddings);

        public async Task<ChromaQuery> QueryCollection(string name, string query, int resultsNumber, Dictionary<string, object> metadataFilters)
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentNullException("No query text has been passed");

            var collection = await _repo.GetCollection(name);

            var embeddingsResult = await _embeddingsService.GenerateEmbeddings(query, collection.DefaultMetadata.DIMENSIONS, collection.DefaultMetadata.MODEL);

            if (!embeddingsResult.GeneratedEmbeddings.Any())
                throw new InvalidDataException($"An error has occured while embedding the query. No generated embeddings found");

            var queryEmbedding = embeddingsResult.GeneratedEmbeddings.First();

            var queryResult = await _repo.QueryCollection(name, queryEmbedding.Vector, resultsNumber, metadataFilters);

            return new ChromaQuery
            {
                Query = query,
                Collection = collection.Name,
                EmbeddingModel = collection.DefaultMetadata.MODEL,
                Dimensions = collection.DefaultMetadata.DIMENSIONS,
                Chunks = queryResult,
                QueryEmbedding = queryEmbedding.Vector
            };
        }

        public async Task<ChromaChunk> InspectChunk(string collectionName, string id)
            => await _repo.GetChunkById(collectionName, id);

        public async Task<SysChunksCollection> CreateSystemChunksCollection(string name, string? description)
            => await _sysRepo.CreateCollection(name, description ?? name, null, null, (int)EChunkType.SYSTEM);
        
        public async Task<ChromaSysChunk> AddSysMessage(string collectionName, ChromaSysChunk chunk, string? version)
        {
            var collection = await _sysRepo.GetCollection(collectionName);

            if (string.IsNullOrEmpty(version))
                version = "v1";

            if (await _sysRepo.ExistsMessage(collection.Name, chunk.Name, version))
                throw new InvalidOperationException($"{collection.Name} >> {nameof(AddSysMessage)} >> A message with name-version: {chunk.Name}-{version} already exists");

            var newChunk = await _sysRepo.DefaultChunk(collectionName);

            var embedding = await _embeddingsService.GenerateEmbeddings(chunk.Name, collection.DefaultMetadata.DIMENSIONS, collection.DefaultMetadata.MODEL);

            if (!embedding.GeneratedEmbeddings.Any())
                throw new InvalidDataException($"{nameof(CreateSystemChunksCollection)} >> collection: {collectionName} >> No generated embeddings");

            newChunk.Text = chunk.Text;
            newChunk.Embedding = embedding.GeneratedEmbeddings.First().Vector;
            newChunk.AddMetadata(nameof(ChromaMetadata.DOCUMENT_NAME).ToLower(), chunk.Name);
            newChunk.AddMetadata(nameof(SysChunkMetadata.TAG).ToLower(), chunk.Tag);

            if (!string.IsNullOrEmpty(version))
                newChunk.AddMetadata(nameof(SysChunkMetadata.VERSION).ToLower(), version);

            return await _sysRepo.InsertChunk(collectionName, newChunk);
        }

        public async Task<ChromaSysChunk> GetSysMessage(string collectionName, string name, string? version = null)
            => await _sysRepo.GetByName(collectionName, name, version);

        public async Task<ChromaSysChunk> DeleteSysMessage(string collectionName, string name, string? version = null)
            => await _sysRepo.DeleteByName(collectionName, name, version);

        public async Task<ChromaSysChunk> DeleteSysMessageById(string collectionName, string id)
        {
            var message = await _sysRepo.GetChunkById(collectionName, id);

            if (!await _sysRepo.DeleteChunk(collectionName, id))
                throw new InvalidOperationException($"{nameof(DeleteSysMessageById)} >> {collectionName} >> An unknown error has occured while deleting system chunk {id}");

            return message;
        }

        public async Task<ChromaSysChunk> PatchSysMessage(string collectionName, string id, string newText, Dictionary<string, object>? metas = null)
        {
            if (string.IsNullOrEmpty(newText))
                throw new BadHttpRequestException($"{nameof(PatchSysMessage)} >> {collectionName} >> No text to update has been sent in the request");

            var message = await _sysRepo.GetChunkById(collectionName, id);

            if (message == null)
                throw new FileNotFoundException($"{nameof(PatchSysMessage)} >> {collectionName} >> No system chunk has been found with ID: {id}");

            var embeddings = await _embeddingsService.GenerateEmbeddings(newText, message.DefaultMetadata.DIMENSIONS, message.DefaultMetadata.MODEL);

            if (!embeddings.GeneratedEmbeddings.Any())
                throw new InvalidDataException($"{nameof(PatchSysMessage)} >> {collectionName} - chunk: {id} >> An error has occured while generating the new text embeddings >> NO GENERATED EMBEDDINGS");

            message.Text = newText;
            message.Embedding = embeddings.GeneratedEmbeddings.First().Vector;

            return await _sysRepo.UpsertChunk(collectionName, message, metas);
        }

        public async Task<ChromaChatsCollection> GetChatsCollection(string name, bool excludeSysmsg = true, string? sessionId = null, int pageSize = 10, int page = 0)
        {
            var collection = await _chatsRepo.GetCollection(name);

            if (string.IsNullOrEmpty(sessionId))
                sessionId = collection.GetMeta<ChatCollectionMetadata>().CURRENT_SESSION_ID;

            if (!collection.GetMeta<ChatCollectionMetadata>().SESSION_IDS.Contains(sessionId))
                throw new BadHttpRequestException($"{nameof(GetChatsCollection)} >> {name} >> Collection has no session with ID: {sessionId}");

            var filters = new Dictionary<string, object> { [nameof(ChatChunkMetadata.SESSION_ID).ToLower()] = sessionId };

            if (excludeSysmsg)
                filters.Add(nameof(ChatChunkMetadata.CHAT_INIT).ToLower(), false);

            return await _chatsRepo.GetCollectionPage(collection.Name, filters, withEmbeddings: false, pageSize, page);
        }

        public async Task<SysChunksCollection> GetSysChunksPage(string collectionName, int pageSize = 10, int page = 0, Dictionary<string, object> filters = null)
            => await _sysRepo.GetCollectionPage(collectionName, filters, withEmbeddings: false, pageSize, page);

        public async Task<ChromaChatsCollection> GetCollectionSessions(string name, int pageSize = 10, int page = 0)
        {
            var collection = await _repo.GetCollection(name);

            return await _chatsRepo.GetCollectionPage(collection.Name, new Dictionary<string, object> { [nameof(ChatChunkMetadata.CHAT_INIT).ToLower()] = true }, withEmbeddings: false, page, pageSize);;
        }

        public async Task<List<ChromaFilesCollection>> GetAllFileCollections()
            => await _fileMgmtService.GetAllCollections();

        public async Task<List<ChromaChatsCollection>> GetAllChatCollections()
            => await _chatsRepo.CollectionsOf((int)EChunkType.CHAT);

        public Task<string> GetSystemInstruction(string collectionName, string messageName)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ILameSearchResult>> SimilaritySearch(string name, ReadOnlyMemory<float> query, int resultsNumber, Dictionary<string, object> filters)
        {
            var results = await _repo.QueryCollection(name, query, resultsNumber, filters);

            return results.Select(chunk => new VectorSearchResult(chunk.Text, (float)chunk.Distance, name, chunk.DefaultMetadata.DOCUMENT_NAME) as ILameSearchResult).ToList();
        }
    }
}
