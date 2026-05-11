using Dotnet.Chroma.Repositories.Models;
using Dotnet.Chroma.Repositories.Models.Interfaces;


namespace DotnetLlamaSharp.Domain.Repositories.Chroma
{
    public interface IChromaChunksRepository : IChromaRepository<ChromaChunksCollection<ChromaChunk>, ChromaChunk> {}
}
