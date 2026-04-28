using DocumentFormat.OpenXml.Spreadsheet;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.DocumentLoader;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.DocumentLoader;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Infrastructure.Exceptions;
using DotnetLlamaSharp.Infrastructure.Services.DocumentLoaders;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Chroma;
using System.Text;

#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace DotnetLlamaSharp.Services.Embeddings
{
    public class DataIngestionService : IDataIngestionService
    {
        private readonly IChromaRepository _chromaRepo;
        private readonly IDocumentLoader<PdfLoaderService> _pdfLoader;
        private readonly IEmbeddingsService _embeddingsService;
        private readonly ApiSettings _settings;
        public DataIngestionService(IChromaRepository chromaRepo, IDocumentLoader<PdfLoaderService> pdfLoader, IEmbeddingsService embeddingsService, IOptions<ApiSettings> settings)
        {
            _chromaRepo = chromaRepo;
            _pdfLoader = pdfLoader;
            _embeddingsService = embeddingsService;
            _settings = settings.Value;
        }

        public async Task<ChromaCollection> CreateCollectionFromFile(CreateCollectionRequest request)
        {
            try
            {
                var document = await _pdfLoader.LoadDocument(request.FileName);
                var collection = await _chromaRepo.CreateCollection(request.Name, request.FileName);
                
                if (collection == null)
                    throw new ArgumentNullException($"An error has occured while creating the collection {request.Name}");

                var chunkPages = new List<DocumentPage>();
                int currentChunkSize = 0;
                var newChunks = new List<ChromaChunk>();
                // await _repo.CreateCollection(request.Name) <- Añade CollectionChunk (ID=0) w/description & metas.totalChunks <-ChunkUpsert tiene que actualizar INCLUYE COL_CHUNK_SIZE & CHUNKED_FILE NAMES
                for (int i = 0; i < document.Pages.Count; i++)
                {
                    var page = document.Pages[i];

                    if (page.Length <= request.ChunkSize)
                    {
                        //Accummulate

                        if(currentChunkSize + page.Length >= request.ChunkSize)
                        {
                            
                            newChunks.AddRange(ChunkPages(chunkPages, request.FileName, request.ChunkSize));
                            chunkPages.Clear();
                            currentChunkSize = 0;
                        }
                        else
                        {
                            currentChunkSize += page.Length;
                            chunkPages.Add(page);
                        }
                    }
                    else
                    {
                        // ProcessCurrent with accumulated
                        
                        if(chunkPages.Count > 0)
                        {
                            newChunks.AddRange(ChunkPages(chunkPages, request.FileName, request.ChunkSize));
                            chunkPages.Clear();
                            currentChunkSize = 0;
                        }
                        var chromaChunks = SplitAndChunk(page, request.FileName, request.ChunkSize);
                        newChunks.AddRange(chromaChunks);
                    }
                }

                //BatchInsert
                var extraMetas = new Dictionary<string, object>();
                extraMetas.Add("file", request.FileName);
                for (int i = 0; i < (int)Math.Ceiling((decimal)(newChunks.Count() / 50)); i++)
                {
                    var batch = newChunks.Skip(i * 50).Take(50).ToList();
                    //EmbeddChunks
                    //Add request (extra) metas
                    await _chromaRepo.InsertChunks(request.Name, batch, bAddTextAsMeta: true, extraMetas: extraMetas);
                }

                var collectionMetas = new Dictionary<string, object>();

                collectionMetas.Add("model", _settings.DefaultEmbedder);
                collectionMetas.Add("dimensions", request.ChunkSize);
                collectionMetas.Add("files", request.FileName);
                return await _chromaRepo.UpdateCollectionData(request.Name, collectionMetas);
            }
            catch(Exception ex)
            {
                await _chromaRepo.DeleteCollection(request.Name);

                throw ex;
                //throw new HttpException($"An error has occured while creating the collection {request.Name} >> {ex.GetType().Name}: {ex.Message}");


            }
        }
        private List<ChromaChunk> ChunkPages(List<DocumentPage> pages, string docName, int chunkSize)
        {
            var chunks = new List<ChromaChunk>();
            var pagesIdx = new List<int>();
            var metadata = new Dictionary<string, object>();
            metadata.Add("document", docName);
            var totalLength = chunks.Sum(c => c.Text.Length);
            if (totalLength > chunkSize)
            {
                int totalNewChunks = (int)Math.Ceiling((decimal)((float)totalLength / (float)chunkSize));

                float chunkLength = totalLength / totalNewChunks;
                int currentChunkSize = 0;
                var chunkBuilder = new StringBuilder();
                pages.ForEach(page =>
                {
                    pagesIdx.Add(page.PageNumber);
                    page.Text.Split(".").ToList().ForEach(sentence => {
                        currentChunkSize += sentence.Length;
                        chunkBuilder
                        .Append(sentence)
                        .Append(".");
                        if (currentChunkSize >= chunkLength)
                        {
                            metadata.Add("pages", pagesIdx);
                            chunks.Add(new ChromaChunk { Text = chunkBuilder.ToString(), Metadata = metadata });
                            currentChunkSize = 0;
                            pagesIdx = new List<int>();
                            chunkBuilder = new StringBuilder();
                        }
                    });
                });
            }
            else
            {
                var sb = new StringBuilder();
                pages.ForEach(page =>
                {
                    sb.Append(page.Text)
                      .Append("\n");

                    if (!pagesIdx.Contains(page.PageNumber))
                        pagesIdx.Add(page.PageNumber);
                });
                metadata.Add("pages", pagesIdx);
                chunks.Add(new ChromaChunk { Text = sb.ToString(), Metadata = metadata });
            }

            return chunks;
        }
       

        private async Task<ChromaChunk> GetUnprocessedChunk(string text, int dimensions, string model)
        {
            var metadata = new Dictionary<string, object>();
            metadata.Add("model", model);
            metadata.Add("size", text.Length);
            metadata.Add("dimensions", dimensions);
            //var embedding = await _embeddingsService.GenerateEmbeddings(text, dimensions, model);
            //if (!embedding.GeneratedEmbeddings.Any())
            //    throw new ArgumentNullException($"An error has occured while generating text embeddings");
            return new ChromaChunk
            {
                Text = text,
                //Embedding = embedding.GeneratedEmbeddings.First().Vector,
                Metadata = metadata
            };
        }
        private List<ChromaChunk> SplitAndChunk(DocumentPage page, string docName,  int chunkSize)
        {
            var chunks = new List<ChromaChunk>();
            var metadata = new Dictionary<string, object>();
            metadata.Add("document", docName);
            metadata.Add("pages", new List<int>(page.PageNumber));
            int totalNewChunks = (int)Math.Ceiling((decimal)((float)page.Text.Length / (float)chunkSize));
            float chunkLength = page.Text.Length / totalNewChunks;
            int currentChunkSize = 0;
            var chunkBuilder = new StringBuilder();
            var currentToken = "";
            page.Text.Split(".").ToList().ForEach(sentence => {
                sentence.Split(" ").ToList().ForEach(token =>
                {
                    currentToken = $" {token}";
                    currentChunkSize += currentToken.Length;
                    chunkBuilder.Append(currentToken);
                    if (currentChunkSize >= chunkLength)
                    {
                        chunks.Add(new ChromaChunk { Text = chunkBuilder.ToString().Trim(), Metadata = metadata });
                        currentChunkSize = 0;
                        chunkBuilder = new StringBuilder();
                    }
                });
                chunkBuilder.Append(".");
            });
            if(currentChunkSize > 0)
                chunks.Add(new ChromaChunk { Text = chunkBuilder.ToString().Trim(), Metadata = metadata });
            return chunks;
        }
    }
}
