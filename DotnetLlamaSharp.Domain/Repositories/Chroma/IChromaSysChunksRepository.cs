using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Repositories.Chroma
{
    public interface IChromaSysChunksRepository : IChromaRepository<SysChunksCollection, ChromaSysChunk>
    {
        Task<SysChunksCollection> CreateCollection(string name, ReadOnlyMemory<float> embedding, string? description);
        Task<ChromaSysChunk> GetByName(string collectionName, string name);
    }
}
