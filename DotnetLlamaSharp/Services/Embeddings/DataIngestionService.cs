using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
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
using System.Net;
using System.Text;

#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace DotnetLlamaSharp.Services.Embeddings
{
    public class DataIngestionService : IDataIngestionService
    {
        private readonly ILogger<DataIngestionService> _logger;
        private readonly IChromaRepository _chromaRepo;
        private readonly IDocumentLoader<PdfLoaderService> _pdfLoader;
        private readonly IEmbeddingsService _embeddingsService;
        private readonly ApiSettings _settings;
        public DataIngestionService(ILogger<DataIngestionService> logger, IChromaRepository chromaRepo, IDocumentLoader<PdfLoaderService> pdfLoader, IEmbeddingsService embeddingsService, IOptions<ApiSettings> settings)
        {
            _logger = logger;
            _chromaRepo = chromaRepo;
            _pdfLoader = pdfLoader;
            _embeddingsService = embeddingsService;
            _settings = settings.Value;
        }

        public async Task<ChromaCollection> CreateCollectionFromFile(EmbedCollectionRequest request)
        {
            try
            {
                /*
                 {
  "name": "RAGS",
  "embeddingModel": "TBD",
  "dimensions": 512,
  "fileName": "BUILDING_AI_AGENTS_WITH_LLMS_RAG_AND_KNOWLEDGE_GRAPHS",
  "chunkSize": 700,
  "chunkOverlap": 20,
  "initialSkip": 3,
  "metadata": {
   
  }
}
                 */
                // Min = 0 --> append 'residual' text to beginning of next chunk
                // Min > 0 --> discards the accumulated text if current document text > chunkSize
                //         --> Min must be >= request chunk overlap
                if (request.MinTextToChunk > 0 && request.MinTextToChunk < request.ChunkOverlap)
                    request.MinTextToChunk = request.ChunkOverlap;

                _logger.LogInformation($"Loading document: {request.FileName}");
                
                var document = await _pdfLoader.LoadDocument(request.FileName);
                
                var collection = await _chromaRepo.CreateCollection(request.Name, request.FileName);
                
                if (collection == null)
                    throw new ArgumentNullException($"An error has occured while creating the collection {request.Name}");

                _logger.LogInformation($"Created collection: {request.Name} for document: {request.FileName}");
                
                var selectedPages = document.Pages.Skip(request.InitialSkip).ToList();

                _logger.LogInformation($"Pages to embed: {selectedPages.Count()} >> SKIPPED INITIAL {request.InitialSkip} PAGES");
                
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
                            chunkPages.Add(page);
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

                _logger.LogInformation($"Generated {newChunks.Count} new chunks from selected document pages");

                request.Metadata.Add(nameof(ChromaMetadata.MODEL).ToLower(), request.EmbeddingModel ?? _settings.DefaultEmbedder);
                request.Metadata.Add(nameof(ChromaMetadata.DIMENSIONS).ToLower(), request.Dimensions);
                //BatchInsert
               
                
                int batchSize = 50;
                var chunkBatches = (int)Math.Ceiling((decimal)(newChunks.Count() / batchSize));

                _logger.LogInformation($"Inserting {chunkBatches} batches >> BATCH SIZE {batchSize}");
                
                int inserted = 0;
                for (int i = 0; i <= chunkBatches; i++)
                {
                    _logger.LogInformation($"Setting batch {i + 1} of {chunkBatches + 1}");

                    var batch = newChunks.Skip(i * batchSize).Take(batchSize).ToList();

                    inserted += await embeddAndStoreChunks(batch, request);

                    _logger.LogInformation($"Inserted {inserted} elements of {newChunks.Count()}");
                }

                request.Metadata.Add(nameof(CollectionMetadata.FILES).ToLower(), request.FileName);
                request.Metadata.Add(nameof(CollectionMetadata.CHUNK_SIZE).ToLower(), request.ChunkSize);
                request.Metadata.Add(nameof(CollectionMetadata.CHUNK_OVERLAP).ToLower(), request.ChunkOverlap);
                request.Metadata.Add(nameof(CollectionMetadata.PAGES).ToLower(), $"{document.Pages.Count}");
                request.Metadata.Add(nameof(CollectionMetadata.SKIPPED_PAGES).ToLower(), request.InitialSkip);
                _logger.LogInformation($"Inserted collection: {request.Name} >> Embedded file: {request.FileName}");
                
                return await _chromaRepo.UpdateCollectionData(request.Name, request.Metadata);
            }
            catch(Exception ex)
            {
                await _chromaRepo.DeleteCollection(request.Name);

                throw ex;
                //throw new HttpException($"An error has occured while creating the collection {request.Name} >> {ex.GetType().Name}: {ex.Message}");


            }
        }

        private async Task<int> embeddAndStoreChunks(List<ChromaChunk> chunks, EmbedCollectionRequest request)
        {
            _logger.LogInformation($"Embedding batch >> EMBEDDING MODEL: {_settings.DefaultEmbedder} >> DIMENSIONS: {request.Dimensions}");
            ChromaChunk currentChunk = null;
            string chunkText = "";
            string textToEmbed = "";
            for (int i = 0; i < chunks.Count; i++)
            {
                textToEmbed = "";
                currentChunk = chunks[i];
                chunkText = currentChunk.Text.Trim();

                if(i == 0)
                    textToEmbed = $"{chunkText} {getNextChunkOverlap(chunks[i + 1], request.ChunkOverlap)}";
                else if(i == chunks.Count -1)
                    textToEmbed = $"{getPreviousChunkOverlap(chunks[i - 1], request.ChunkOverlap)} {chunkText}";
                else
                    textToEmbed = $"{getPreviousChunkOverlap(chunks[i - 1], request.ChunkOverlap)} {chunkText} {getNextChunkOverlap(chunks[i + 1], request.ChunkOverlap)}";

                var model = request.EmbeddingModel ?? _settings.DefaultEmbedder;

                if (!_settings.EmbeddingModels.Contains(model))
                    throw new HttpException(HttpStatusCode.BadRequest, $"Embedding model: {model} not available");

                var embedding = await _embeddingsService.GenerateEmbeddings(textToEmbed, request.Dimensions, model);

                currentChunk.Embedding = embedding.GeneratedEmbeddings.First().Vector;

                request.Metadata.Keys.ToList().ForEach(key => currentChunk.AddMetadata(key.ToLower(), request.Metadata[key]));
            }

            return await _chromaRepo.InsertChunks(request.Name, chunks, bAddTextAsMeta: true, extraMetas: request.Metadata);
        }

        private string getPreviousChunkOverlap(ChromaChunk prevChunk, int overlappedTokens)
        {
            var text = prevChunk.Text.Replace("\n", "");
            var sentences = text.Split(".").Where(sentence => !string.IsNullOrEmpty(sentence)).ToList();
            if (text.EndsWith("."))
                sentences[sentences.Count - 1] += ".";

            var tokens = new List<string>();
            for (int i = sentences.Count - 1; i >= 0; i--)
            {
                var sentence = sentences[i].Trim();

                var sentenceTokens = sentence.Split(" ").ToList();
                
                for (int j = sentenceTokens.Count - 1; j >= 0; j--)
                {
                    if (sentenceTokens[j].StartsWith("\\")) continue;

                    if (tokens.Count >= overlappedTokens) break;

                    if (j == sentenceTokens.Count -1 && i != sentences.Count -1)
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

                if (string.IsNullOrEmpty(sentence)) continue;

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
            metadata.Add(nameof(ChunkMetadata.DOCUMENT).ToLower(), docName);
            var totalLength = pages.Sum(c => c.Text.Length);
            if (totalLength > chunkSize)
            {
                int totalNewChunks = (int)Math.Ceiling((decimal)((float)totalLength / (float)chunkSize));

                float chunkLength = totalLength / totalNewChunks;
                int currentChunkSize = 0;
                var chunkBuilder = new StringBuilder();
                //CHUNK BY TOKEN COUNT
                pages.ForEach(page =>
                {
                    pagesIdx.Add(page.PageNumber);
                    var sentences = page.Text.Split(".").ToList();
                    for (int i = 0; i < sentences.Count; i++) 
                    {
                        var sentence = sentences[i].Trim().Replace("\n", "");
                        
                        if (string.IsNullOrEmpty(sentence)) continue;

                        var tokens = sentence.Split(" ").ToList();

                        for(int j = 0; j < tokens.Count; j++)
                        {
                            if (tokens[j].StartsWith("\\")) continue;

                            if (currentChunkSize >= chunkLength)
                            {
                                var chunkedPagesIds = "";
                                pagesIdx.ForEach(idx => {
                                    chunkedPagesIds += $"{idx},";
                                    metadata.Add($"page_{idx}", true);
                                });
                                metadata.Add(nameof(ChunkMetadata.PAGES).ToLower(), chunkedPagesIds.Substring(0, chunkedPagesIds.Length -1));
                                chunks.Add(new ChromaChunk { Text = chunkBuilder.ToString(), Metadata = metadata });
                                currentChunkSize = 0;
                                pagesIdx = new List<int>();
                                chunkBuilder = new StringBuilder();
                            }
                            else
                            {
                                currentChunkSize += tokens[j].Length;
                                chunkBuilder.Append($" {tokens[j]}");
                            }
                        };

                        if (currentChunkSize >= chunkLength) break;

                        chunkBuilder.Append(".");
                    };
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
                var chunkedPagesIds = "";
                pagesIdx.ForEach(idx => {
                    chunkedPagesIds += $"{idx},";
                    metadata.Add($"page_{idx}", true);
                });
                metadata.Add(nameof(ChunkMetadata.PAGES).ToLower(), chunkedPagesIds.Substring(0, chunkedPagesIds.Length -1));
                chunks.Add(new ChromaChunk { Text = sb.ToString(), Metadata = metadata });
            }

            return chunks;
        }
       
        private List<ChromaChunk> SplitAndChunk(DocumentPage page, string docName,  int chunkSize)
        {
            var chunks = new List<ChromaChunk>();
            var metadata = new Dictionary<string, object>();
            metadata.Add(nameof(ChunkMetadata.DOCUMENT).ToLower(), docName);
            metadata.Add(nameof(ChunkMetadata.PAGES).ToLower(), $"{page.PageNumber}");
            metadata.Add($"page_{page.PageNumber}", true);

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
