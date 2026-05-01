using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Chroma;

namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{
    public class ChromaFilesRepository : ChromaBaseRepository<ChromaFilesCollection, ChromaFileChunk>, IChromaGenericRepository<ChromaFilesCollection, ChromaFileChunk>
    {
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public ChromaFilesRepository(ILogger<ChromaRepository> logger, IOptions<ApiSettings> dbSettings, IChromaClient client) : base(logger, dbSettings, client)

        {
        }
    }
}
