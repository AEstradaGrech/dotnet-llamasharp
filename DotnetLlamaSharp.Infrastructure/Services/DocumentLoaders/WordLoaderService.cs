using DocumentFormat.OpenXml.Packaging;
using DotnetLlamaSharp.Domain.Models.Primitives.DocumentLoader;
using DotnetLlamaSharp.Domain.Services.DocumentLoader;
using System.Text;

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
            var fullPath = Path.Combine(_basePath, $"{fileName}.docx");

            if (!File.Exists(fullPath))
                throw new FileNotFoundException(fileName);

            var sb = new StringBuilder();

            using var doc = WordprocessingDocument.Open(fullPath, false);
            
            var body = doc.MainDocumentPart.Document.Body;

            int index = 0;
            var pages = new List<DocumentPage>();
            
            foreach (var paragraph in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                var text = paragraph.InnerText?.Trim();

                if (string.IsNullOrWhiteSpace(text)) continue;

                pages.Add(new DocumentPage(index++, text));
            }

            return Task.FromResult(new Document(fileName, "word", index, pages));
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
