using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Repositories.Chroma
{
    public interface IChromaChatsRepository : IChromaRepository<ChromaChatCollection, ChromaChatChunk>
    {
        Task<ChromaChatCollection> InitCollection(string agentName, string userName, string? description);
        Task<ChromaChatChunk> GetCurrentSessionChunk(string collection);
    }
}
