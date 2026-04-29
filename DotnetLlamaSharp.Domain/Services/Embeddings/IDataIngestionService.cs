using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Request;

namespace DotnetLlamaSharp.Domain.Services.Embeddings
{
    public interface IDataIngestionService
    {

        Task<ChromaCollection> CreateCollectionFromFile(EmbedCollectionRequest request);


    }
}
