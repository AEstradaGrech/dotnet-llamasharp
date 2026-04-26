
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Chroma;

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
            => throw new NotImplementedException();

        public async Task<string> CreateCollection(string collectionName)
        {
            try
            {
                await _dbClient.CreateCollectionAsync(collectionName);
                return collectionName;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ChromaRepository)} >> {nameof(CreateCollection)} >> {ex.Message}");
                throw new InvalidOperationException($"An error has occured while creating the ChromaDB collection: {collectionName}");
            }
        }
    }
}
