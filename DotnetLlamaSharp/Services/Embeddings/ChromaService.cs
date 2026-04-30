using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Embeddings;

namespace DotnetLlamaSharp.Services.Embeddings
{
    public class ChromaService : IChromaService
    {
        private readonly ILogger<ChromaService> _logger;
        private readonly IDataIngestionService _ingestionService;
        private readonly IChromaRepository _repo;


        public ChromaService(ILogger<ChromaService> logger, IDataIngestionService ingestionService, IChromaRepository repo)
        {
            _logger = logger;
            _ingestionService = ingestionService;
            _repo = repo;
        }

        public async Task<ChromaCollection> CreateCollection(CreateCollectionRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<ChromaCollection> CreateCollectionFromFile(EmbedCollectionRequest request)
            => await _ingestionService.CreateCollectionFromFile(request);

        public async Task<bool> DeleteCollection(string name)
            => await _repo.DeleteCollection(name);

        public async Task<ChromaCollection> GetCollection(string name)
            => await _repo.GetCollection(name);

        public Task<IAsyncEnumerable<string>> GetDbCollections()
            => _repo.GetDbCollections();

        public async Task<ChunksCollection> InspectCollection(string name, int startIndex = 0, int samples = 0, bool includeEmbeddings = false)
        {
            var collection = await GetCollection(name);

            var ids = new List<string>();

            if (startIndex > collection.DefaultMetadata.CHUNKS)
                throw new InvalidOperationException($"The requested chunk samples start index is grater or equal to the number of available chunks ({collection.DefaultMetadata.CHUNKS})");

            if (startIndex < 0)
                startIndex = 0;

            if (samples == 0 || samples > collection.DefaultMetadata.CHUNKS)
                samples = collection.DefaultMetadata.CHUNKS;

            for (int i = startIndex; i < startIndex + samples; i++)
                if(i <= collection.DefaultMetadata.CHUNKS)
                    ids.Add($"{i}");
            
            return await _repo.InspectCollection(name, ids, includeEmbeddings);
        }
    }
}
