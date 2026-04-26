using DotnetLlamaSharp.Domain.Models.Primitives.DocumentLoader;


namespace DotnetLlamaSharp.Domain.Services.DocumentLoader
{
    public interface IDocumentLoader<T> where T : BaseDocumentLoader
    {
        Task<Document> LoadDocument(string fileName);
        Task<DocumentPage> LoadPage(string fileName, int page);
        Task<List<DocumentPage>> LoadPages(string fileName, int startIndex, int batchSize);
    }
}
