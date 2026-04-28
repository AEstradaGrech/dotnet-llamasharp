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
                for (int i = 0; i < document.TotalPages; i++)
                {
                    var page = document.Pages[i];

                    if (page.Length <= request.ChunkSize)
                    {
                        if (currentChunkSize + page.Length > request.ChunkSize)
                        {
                            newChunks.Add(await EmbedChunkFromPages(chunkPages, request.ChunkSize, _settings.DefaultEmbedder));
                            chunkPages.Clear();
                            chunkPages.Add(page);
                            continue;
                        }
                        else
                        {
                            currentChunkSize += page.Length;
                            chunkPages.Add(page);
                        }
                    }
                    else newChunks.AddRange(await SplitAndEmbed(page, request.ChunkSize, _settings.DefaultModel));
                }

                //BatchInsert
                var extraMetas = new Dictionary<string, object>();
                extraMetas.Add("file", request.FileName);
                for (int i = 0; i < (int)Math.Ceiling((decimal)(newChunks.Count() / 50)); i++)
                {
                    var batch = newChunks.Skip(i * 50).Take(50).ToList();

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

        private async Task<ChromaChunk> EmbedChunkFromPages(List<DocumentPage> pages, int embeddingsDimensions, string model)
        {
            var sb = new StringBuilder();
            var metadata = new Dictionary<string, object>();
            var pagesTag = "";
            metadata.Add("pages", pagesTag);
            var size = 0;
            pages.ForEach(page =>
            {
                sb.Append(page.Text.Trim())
                  .Append("\n");
                size += page.Length;
                pagesTag += $"{page.PageNumber}, ";
            });

            var newChunk = await GetEmbeddedChunk(sb.ToString().Trim(), embeddingsDimensions, model);

            newChunk.Metadata["pages"] = pagesTag.Trim().Substring(0, pagesTag.Length - 1);
            
            return newChunk;
        }

        private async Task<ChromaChunk> GetEmbeddedChunk(string text, int dimensions, string model)
        {
            var metadata = new Dictionary<string, object>();
            metadata.Add("model", model);
            metadata.Add("size", text.Length);
            metadata.Add("dimensions", dimensions);
            var embedding = await _embeddingsService.GenerateEmbeddings(text, dimensions, model);
            if (!embedding.GeneratedEmbeddings.Any())
                throw new ArgumentNullException($"An error has occured while generating text embeddings");
            return new ChromaChunk
            {
                Text = text,
                Embedding = embedding.GeneratedEmbeddings.First().Vector,
                Metadata = metadata
            };
        }
        private async Task<List<ChromaChunk>> SplitAndEmbed(DocumentPage page, int dimensions, string model)
        {
            var newChunks = new List<ChromaChunk>();

            if(page.Length <= dimensions * 2f)
            {
                for(int i = 0; i<2; i++)
                {
                    var text = page.Text.Substring(0 * dimensions, i == 0 ? dimensions : dimensions * i);

                    var newChunk = await GetEmbeddedChunk(text, dimensions, model);

                    newChunk.Metadata.Add("pages", $"{page.PageNumber}");
                    newChunk.Metadata.Add("page_part", i);

                    newChunks.Add(newChunk);
                }
            }
            else
            {
                var chunks = (float)Math.Ceiling((decimal)(page.Length / dimensions));
                
                for (int i = 0; i < chunks; i++)
                {
                    int substringEnd = i == 0 ? dimensions : i * dimensions * 2;
                    
                    if (substringEnd > page.Length)
                        substringEnd = page.Length;

                    var text = page.Text.Substring(i * dimensions, substringEnd);

                    var newChunk = await GetEmbeddedChunk(text, dimensions, model);

                    newChunk.Metadata.Add("pages", $"{page.PageNumber}");
                    newChunk.Metadata.Add("page_part", i);

                    newChunks.Add(newChunk);
                }
            }

            return newChunks;
        }
    }
}
