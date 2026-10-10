using Dotnet.Chroma.Repositories;
using Dotnet.Chroma.Repositories.Interfaces;
using Dotnet.Chroma.Repositories.Models.Settings;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{
    public class ChromaFilesRepository : ChromaRepository<ChromaFilesCollection, ChromaFileChunk>, IChromaFilesRepository
    {
        public ChromaFilesRepository(ILogger<ChromaFilesRepository> logger, IOptions<ChromaSettings> dbSettings, IChromaDbClient dbClient) : base(dbClient, dbSettings) { }

        public override async Task<IEnumerable<string>> GetDbCollections()
        {
            var collections = await CollectionsOf((int)EChunkType.FILE);

            return collections.Count > 0 ? collections.Select(c => c.Name) : Enumerable.Empty<string>();
        }
    }
}
