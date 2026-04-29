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
    }
}
