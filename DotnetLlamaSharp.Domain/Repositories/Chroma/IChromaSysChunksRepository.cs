using Dotnet.Chroma.Repositories.Models.Interfaces;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;

namespace DotnetLlamaSharp.Domain.Repositories.Chroma
{
    public interface IChromaSysChunksRepository : IChromaRepository<SysChunksCollection, ChromaSysChunk>
    {
        Task<SysChunksCollection> CreateCollection(string name, string? description, string? embeddingModel = null, int? embeddingDimensions = null);
        Task<bool> ExistsMessage(string collectionName, string name, string? version);
        Task<ChromaSysChunk> GetByName(string collectionName, string name, string? version = null);
        Task<ChromaSysChunk> DeleteByName(string collectionName, string name, string? version = null);
        Task<string> GetSystemMessage(string collectionName, string name);
    }
}
