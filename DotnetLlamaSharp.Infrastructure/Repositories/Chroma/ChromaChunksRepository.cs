using Dotnet.Chroma.Repositories;
using Dotnet.Chroma.Repositories.Models;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Chroma;

namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class ChromaChunksRepository : ChromaRepository<ChromaChunksCollection<ChromaChunk>, ChromaChunk>, IChromaChunksRepository
    {
        public ChromaChunksRepository(ILogger<ChromaChunksRepository> logger, IOptions<ChromaSettings> dbSettings, IChromaClient client) : base(client, dbSettings) { }
    }
}
