using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Chroma;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{
    public class ChromaChatsRepository : ChromaRepository<ChromaChatCollection, ChromaChatChunk>, IChromaChatsRepository
    {
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public ChromaChatsRepository(ILogger<ChromaChatsRepository> logger, IOptions<ApiSettings> dbSettings, IChromaClient client) : base(logger, dbSettings, client)
        {
        }

        public async Task<ChromaChatChunk> GetSessionChunk(string collection)
            => await GetChunkById(collection, "1");
    }
}
