using DotnetLlamaSharp.Domain.Models.Primitives.DocumentLoader;
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
        private readonly IEmbeddingsService _embeddingsService;
        public DataIngestionService(IChromaRepository chromaRepo, IDocumentLoader<PdfLoaderService> pdfLoader, IEmbeddingsService embeddingsService)
        {
            _chromaRepo = chromaRepo;
            _pdfLoader = pdfLoader;
            _embeddingsService = embeddingsService;
        }

        public async Task<ChromaCollectionModel> CreateCollectionFromFile(CreateCollectionRequest request)
        {
            var document = await _pdfLoader.LoadDocument(request.FileName);

            var chunkPages = new List<DocumentPage>();
            int currentChunkSize = 0;
            // await _repo.CreateCollection(request.Name) <- Añade CollectionChunk (ID=0) w/description & metas.totalChunks <-ChunkUpsert tiene que actualizar INCLUYE COL_CHUNK_SIZE & CHUNKED_FILE NAMES
            for(int i= 0; i < document.TotalPages; i++)
            {
                var page = document.Pages[i];

                if(page.Length > request.ChunkSize)
                {
                    // 1a pagina = N chunks (pagina 1000 chars w/ChunkSize = 256
                    // SplitAndStore(page)
                }
                else
                {
                    if(currentChunkSize + page.Length > request.ChunkSize)
                    {
                        // pagesToChunkAndFlush(ref currentChunkPages)
                        // chunkPages.Add(page);
                        // continue;
                    }
                    else
                    {
                        currentChunkSize += page.Length;
                        chunkPages.Add(page);
                        // v1 -> pasando de overlapOffset
                        if(currentChunkSize >= request.ChunkSize) // ChunkSize * threshold [0.9 | 0.8]
                        {
                            // pages to ChromaChunk <- metadatas += pagesIds
                            // V2 --> https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.text.textchunker.splitplaintextparagraphs?view=semantic-kernel-dotnet
                            // newChunk.Embedding = embedChunkText(newChunk.Text)
                            // await _repo.CreateChunk(newChunk)
                        }
                    }

                }
                
            }
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
