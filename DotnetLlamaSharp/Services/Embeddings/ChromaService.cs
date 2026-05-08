using AutoMapper;
using DocumentFormat.OpenXml.Wordprocessing;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Request;
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
            => await _sysRepo.CreateCollection(name, description ?? name);
        
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
    }
}
