using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2016.Excel;
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
                // Min = 0 --> append 'residual' text to beginning of next chunk
                // Min > 0 --> discards the accumulated text if current document text > chunkSize
                //         --> Min must be >= request chunk overlap
                if (request.MinTextToChunk > 0 && request.MinTextToChunk < request.ChunkOverlap)
                    request.MinTextToChunk = request.ChunkOverlap;

                var document = await _pdfLoader.LoadDocument(request.FileName);
                var collection = await _chromaRepo.CreateCollection(request.Name, request.FileName);
                
                if (collection == null)
                    throw new ArgumentNullException($"An error has occured while creating the collection {request.Name}");

                var selectedPages = document.Pages.Skip(request.InitialSkip).ToList();
                var chunkPages = new List<DocumentPage>();
                int currentChunkSize = 0;
                var newChunks = new List<ChromaChunk>();
                // await _repo.CreateCollection(request.Name) <- Añade CollectionChunk (ID=0) w/description & metas.totalChunks <-ChunkUpsert tiene que actualizar INCLUYE COL_CHUNK_SIZE & CHUNKED_FILE NAMES
                for (int i = 0; i < selectedPages.Count(); i++)
                {
                    var page = selectedPages[i];

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
                        var chromaChunks = SplitAndChunk(page, request.FileName, request.ChunkSize);

                        if (chunkPages.Count > 0)
                        {
                            if(currentChunkSize > request.MinTextToChunk)
                            {
                                newChunks.AddRange(ChunkPages(chunkPages, request.FileName, request.ChunkSize));
                            }
                            else
                            {
                                var residualChunk = ChunkPages(chunkPages, request.FileName, request.ChunkSize).First();

                                newChunks.First().Text = $"{residualChunk.Text}\n\n{newChunks.First().Text}";
                            }

                            chunkPages.Clear();
                            currentChunkSize = 0;
                        }

                        newChunks.AddRange(chromaChunks);
                    }
                }

                request.Metadata.Add("model", _settings.DefaultEmbedder);
                request.Metadata.Add("dimensions", request.ChunkSize);
                //BatchInsert
                var chunkBatches = (int)Math.Ceiling((decimal)(newChunks.Count() / 50));
                int inserted = 0;
                for (int i = 0; i < chunkBatches; i++)
                {
                    var batch = newChunks.Skip(i * 50).Take(50).ToList();

                    inserted += await embeddAndStoreChunks(batch, request);
                }

                var collectionMetas = new Dictionary<string, object>();
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

        private async Task<int> embeddAndStoreChunks(List<ChromaChunk> chunks, CreateCollectionRequest request)
        {
            ChromaChunk currentChunk = null;
            string chunkText = "";
            string textToEmbed = "";
            for (int i = 0; i < chunks.Count; i++)
            {
                textToEmbed = "";
                currentChunk = chunks[i];
                chunkText = currentChunk.Text.Trim();

                if (i == 0)
                    textToEmbed = $"{chunkText} {getNextChunkOverlap(chunks[i + 1], request.ChunkOverlap)}";
                
                else if (i == chunks.Count - 1)
                    textToEmbed = $"{getPreviousChunkOverlap(chunks[i - 1], request.ChunkOverlap)} {chunkText}";
                
                else textToEmbed = $"{getPreviousChunkOverlap(chunks[i - 1], request.ChunkOverlap)} {chunkText} {getNextChunkOverlap(chunks[i + 1], request.ChunkOverlap)}";

                var embedding = await _embeddingsService.GenerateEmbeddings(textToEmbed, request.Dimensions, _settings.DefaultEmbedder);

                currentChunk.Embedding = embedding.GeneratedEmbeddings.First().Vector;

                request.Metadata.Keys.ToList().ForEach(key => {
                    if (currentChunk.Metadata.ContainsKey(key))
                        currentChunk.Metadata.Add(key, request.Metadata[key]);
                });
            }

            return await _chromaRepo.InsertChunks(request.Name, chunks, bAddTextAsMeta: true, extraMetas: request.Metadata);
        }

        private string getPreviousChunkOverlap(ChromaChunk prevChunk, int overlappedTokens)
        {
            var text = prevChunk.Text.Replace("\n", "");
            var sentences = text.Split(".").Where(sentence => !string.IsNullOrEmpty(sentence)).ToList();
            var tokens = new List<string>();
            for (int i = sentences.Count - 1; i >= 0; i--)
            {
                var sentence = sentences[i].Trim();

                var sentenceTokens = sentence.Split(" ").ToList();
                
                for (int j = sentenceTokens.Count - 1; j >= 0; j--)
                {
                    if (sentenceTokens[j].StartsWith("\\")) continue;

                    if (tokens.Count >= overlappedTokens) break;

                    if (j == sentenceTokens.Count -1)
                        sentenceTokens[j] += ".";

                    tokens.Add(sentenceTokens[j]);
                }

                if (tokens.Count >= overlappedTokens) break;
            }
            tokens.Reverse();
            var sb = new StringBuilder();
            tokens.ForEach(token => sb.Append(" ").Append(token));
            return sb.ToString().Trim();
        }

        private string getNextChunkOverlap(ChromaChunk nextChunk, int overlappedTokens)
        {
            var text = nextChunk.Text.Replace("\n", "");
            var sentences = nextChunk.Text.Split(".").Where(sentence => !string.IsNullOrEmpty(sentence)).ToList();
            var tokens = new List<string>();
            for(int i = 0; i < sentences.Count; i++)
            {
                var sentence = sentences[i].Trim();

                var sentenceTokens = sentence.Split(" ").ToList();

                for (int j = 0; j < sentenceTokens.Count; j++)
                {
                    if (sentenceTokens[j].StartsWith("\\")) continue;

                    if (tokens.Count >= overlappedTokens) break;

                    if (j == sentenceTokens.Count - 1)
                        sentenceTokens[j] += ".";

                    if (tokens.Count <= overlappedTokens)
                        tokens.Add(sentenceTokens[j]);
                }

                if (tokens.Count >= overlappedTokens) break;
            }
            var sb = new StringBuilder();
            tokens.ForEach(token => sb.Append(" ").Append(token));
            return sb.ToString().Trim();
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
