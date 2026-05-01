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
        private readonly IDataIngestionService _ingestionService;
        private readonly IEmbeddingsService _embeddingsService;
        private readonly IChromaRepository _repo;


        public ChromaService(ILogger<ChromaService> logger, IDataIngestionService ingestionService, IEmbeddingsService embeddingsService, IChromaRepository repo)
        {
            _logger = logger;
            _ingestionService = ingestionService;
            _embeddingsService = embeddingsService;
            _repo = repo;
        }

        public async Task<ChromaFilesCollection> CreateCollection(CreateCollectionRequest request)
        {
            await _repo.CreateCollection(request.Name, request.Description ?? request.Name, request.EmbeddingModel, request.Dimensions);

            return await _repo.GetCollection(request.Name);
        }

        public async Task<ChromaFilesCollection> CreateCollectionFromFile(EmbedCollectionRequest request)
            => await _ingestionService.CreateCollectionFromFile(request);

        public async Task<bool> DeleteCollection(string name)
            => await _repo.DeleteCollection(name);

        public async Task<ChromaFilesCollection> GetCollection(string name)
            => await _repo.GetCollection(name);

        public Task<IAsyncEnumerable<string>> GetDbCollections()
            => _repo.GetDbCollections();

        //public async Task<FileChunksCollection> InspectCollection(string name, int startIndex = 0, int samples = 0, bool includeEmbeddings = false)
        //{
        //    var collection = await GetCollection(name);

        //    var ids = new List<string>();

        //    if (startIndex > collection.DefaultMetadata.CHUNKS)
        //        throw new InvalidOperationException($"The requested chunk samples start index is grater or equal to the number of available chunks ({collection.DefaultMetadata.CHUNKS})");

        //    if (startIndex < 1)
        //        startIndex = 1;

        //    if (samples == 0 || samples > collection.DefaultMetadata.CHUNKS)
        //        samples = collection.DefaultMetadata.CHUNKS;

        //    for (int i = startIndex; i < startIndex + samples; i++)
        //        if(i <= collection.DefaultMetadata.CHUNKS)
        //            ids.Add($"{i}");
            
        //    return await _repo.InspectCollection(name, ids, includeEmbeddings);
        //}

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
    }
}
