using AutoMapper;
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


        public ChromaService(ILogger<ChromaService> logger, IChromaFilesService ingestionService, IEmbeddingsService embeddingsService, IChromaChunksRepository repo)
        {
            _logger = logger;
            _fileMgmtService = ingestionService;
            _embeddingsService = embeddingsService;
            _repo = repo;
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
    }
}
