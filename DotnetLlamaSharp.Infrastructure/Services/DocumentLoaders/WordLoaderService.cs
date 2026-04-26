using DotnetLlamaSharp.Domain.Models.Primitives.DocumentLoader;
using DotnetLlamaSharp.Domain.Services.DocumentLoader;

namespace DotnetLlamaSharp.Infrastructure.Services.DocumentLoaders
{
    public class WordLoaderService : BaseDocumentLoader, IDocumentLoader<WordLoaderService>
    {
        private const string LOADER = "words";
        public WordLoaderService() : base(docsFolder: LOADER)
        {
            validate(LOADER);
        }

        public override Task<Document> LoadDocument(string fileName)
        {
            throw new NotImplementedException();
        }

        public override Task<DocumentPage> LoadPage(string fileName, int page)
        {
            throw new NotImplementedException();
        }

        public override Task<List<DocumentPage>> LoadPages(string fileName, int startIndex, int batchSize)
        {
            throw new NotImplementedException();
        }
    }
}
