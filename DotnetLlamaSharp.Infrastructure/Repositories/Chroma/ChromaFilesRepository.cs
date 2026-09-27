using Dotnet.Chroma.Repositories;
using Dotnet.Chroma.Repositories.Interfaces;
using Dotnet.Chroma.Repositories.Models.Settings;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{
    public class ChromaFilesRepository : ChromaRepository<ChromaFilesCollection, ChromaFileChunk>, IChromaFilesRepository
    {
        public ChromaFilesRepository(ILogger<ChromaFilesRepository> logger, IOptions<ChromaSettings> dbSettings, IChromaDbClient dbClient) : base(dbClient, dbSettings) { }
    }
}
