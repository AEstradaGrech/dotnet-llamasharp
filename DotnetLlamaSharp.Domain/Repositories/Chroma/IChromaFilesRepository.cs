using Dotnet.Chroma.Repositories.Models.Interfaces;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;

namespace DotnetLlamaSharp.Domain.Repositories.Chroma
{
    public interface IChromaFilesRepository : IChromaRepository<ChromaFilesCollection, ChromaFileChunk> {}
}
