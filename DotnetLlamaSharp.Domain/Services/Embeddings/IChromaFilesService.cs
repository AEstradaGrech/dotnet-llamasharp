using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Request.Chroma;

namespace DotnetLlamaSharp.Domain.Services.Embeddings
{
    public interface IChromaFilesService
    {

        Task<ChromaFilesCollection> CreateCollectionFromFile(EmbedCollectionRequest request);
        Task<ChromaFilesCollection> CreateEmptyFileCollection(CreateCollectionRequest request);
        Task<ChromaFilesCollection> InspectCollection(string name, int startIndex = 0, int samples = 0, bool includeEmbeddings = false); // Default: all chunks from idx 0
        Task<List<ChromaFilesCollection>> GetAllCollections();

    }
}
