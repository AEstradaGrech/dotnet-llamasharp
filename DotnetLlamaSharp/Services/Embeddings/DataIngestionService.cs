using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.DocumentLoader;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Infrastructure.Services.DocumentLoaders;
using Microsoft.SemanticKernel.Connectors.Chroma;

#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace DotnetLlamaSharp.Services.Embeddings
{
    public class DataIngestionService : IDataIngestionService
    {
        private readonly IChromaRepository _chromaRepo;
        private readonly IDocumentLoader<PdfLoaderService> _pdfLoader;

        public DataIngestionService(IChromaRepository chromaRepo, IDocumentLoader<PdfLoaderService> pdfLoader)
        {
            _chromaRepo = chromaRepo;
            _pdfLoader = pdfLoader;
        }

        public async Task<ChromaCollectionModel> CreateCollectionFromFile(CreateCollectionRequest request)
        {
            var document = await _pdfLoader.LoadDocument(request.FileName);

            // if document.Pages <= 0) throw ex
            // document.Pages.Foreach(page =>
            //  chunk.id = page.Number
            //  var embedding = _embeddings.GenerateEmbeddings(page.Text, request.Model, request.Dimensions)
            //  chunk.Metas = request.Metas
            //  _repo.UpsertChunk(request.Name, chunk)
            throw new NotImplementedException();
        }
    }
}
