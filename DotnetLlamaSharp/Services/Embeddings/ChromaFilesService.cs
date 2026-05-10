using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.DocumentLoader;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.DocumentLoader;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Infrastructure.Exceptions;
using DotnetLlamaSharp.Infrastructure.Services.DocumentLoaders;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace DotnetLlamaSharp.Services.Embeddings
{
    public class ChromaFilesService : IChromaFilesService
    {
        private readonly ILogger<ChromaFilesService> _logger;
        private readonly IChromaFilesRepository _repo;
        private readonly IDocumentLoader<PdfLoaderService> _pdfLoader;
        private readonly IEmbeddingsService _embeddingsService;
        private readonly ApiSettings _settings;
        public ChromaFilesService(ILogger<ChromaFilesService> logger, IChromaFilesRepository chromaRepo, IDocumentLoader<PdfLoaderService> pdfLoader, IEmbeddingsService embeddingsService, IOptions<ApiSettings> settings)
        {
            _logger = logger;
            _repo = chromaRepo;
            _pdfLoader = pdfLoader;
            _embeddingsService = embeddingsService;
            _settings = settings.Value;
        }

        public async Task<ChromaFilesCollection> CreateCollectionFromFile(EmbedCollectionRequest request)
        {
            try
            {
                /*
                 {
  "name": "rags",
  "dimensions": 512,
  "fileName": "BUILDING_AI_AGENTS_WITH_LLMS_RAG_AND_KNOWLEDGE_GRAPHS",
  "fileExtension" : "pdf",
  "chunkSize": 800,
  "chunkOverlap": 40,
  "initialSkip": 20,
  "pageCutoff" : 10,
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

                bool isUpdate = await _repo.CollectionExists(request.Name);

                if(string.IsNullOrEmpty(request.EmbeddingModel))
                    request.EmbeddingModel = _settings.DefaultEmbedder;

                ChromaFilesCollection collection = isUpdate ?
                    await _repo.GetCollection(request.Name) :
                    await _repo.CreateCollection(request.Name, request.Description ?? request.FileName, request.EmbeddingModel, request.Dimensions);
                
                if (collection == null)
                    throw new ArgumentNullException($"An error has occured while creating the collection {request.Name}");

                if(isUpdate)
                {
                    if (collection.GetMeta<FileCollectionMetadata>().FILES.Contains(request.FileName))
                        throw new HttpException(HttpStatusCode.BadRequest, $"Collection: {request.Name} already contains file {request.FileName}");

                    request.EmbeddingModel = collection.GetMeta<FileCollectionMetadata>().MODEL;
                    request.Dimensions = collection.GetMeta<FileCollectionMetadata>().DIMENSIONS;
                }

                else request.Metadata.Add(nameof(ChromaCollectionMetadata.CHUNK_TYPE).ToLower(), (int)EChunkType.FILE);
                
                _logger.LogInformation($"Beginning data ingestion process for collection: {request.Name} for document: {request.FileName}");

                if (request.PageCutoff >= document.Pages.Count())
                    throw new InvalidOperationException($"The selected cutoff ({request.PageCutoff}) is greater or equal to the total document pages ({document.Pages.Count()})");

                if (request.InitialSkip >= document.Pages.Count())
                    throw new InvalidOperationException($"The selected initial skip ({request.InitialSkip}) is greater or equal to the total document pages ({document.Pages.Count()})");

                if (request.InitialSkip + request.PageCutoff >= document.Pages.Count())
                    throw new InvalidOperationException($"Invalid document pages span. Initial Skip ({request.InitialSkip}) and Page Cutoff ({request.PageCutoff}) exceeds the number of available pages ({document.Pages.Count()})");

                var selectedPages = document.Pages.Skip(request.InitialSkip).Take(document.Pages.Count() - (request.InitialSkip + request.PageCutoff)).ToList();

                _logger.LogInformation($"Pages to embed: {selectedPages.Count()} >> SKIPPED INITIAL {request.InitialSkip} PAGES");
                
                var chunkPages = new List<DocumentPage>();
                int currentChunkSize = 0;
                var newChunks = new List<ChromaChunk>();
                for (int i = 0; i < selectedPages.Count(); i++)
                {
                    var page = selectedPages[i];

                    if (page.Length <= request.ChunkSize)
                    {
                        if(currentChunkSize + page.Length >= request.ChunkSize)
                        {
                            chunkPages.Add(page);
                            newChunks.AddRange(phunkPages(chunkPages, request.FileName, request.ChunkSize));
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
                        var chromaChunks = splitAndChunk(page, request.FileName, request.ChunkSize);

                        if (chunkPages.Count > 0)
                        {
                            if(currentChunkSize > request.MinTextToChunk)
                            {
                                newChunks.AddRange(phunkPages(chunkPages, request.FileName, request.ChunkSize));
                            }
                            else
                            {
                                var residualChunk = phunkPages(chunkPages, request.FileName, request.ChunkSize).First();

                                newChunks.First().Text = $"{residualChunk.Text}\n\n{newChunks.First().Text}";
                            }

                            chunkPages.Clear();
                            currentChunkSize = 0;
                        }

                        newChunks.AddRange(chromaChunks);
                    }
                }

                _logger.LogInformation($"Generated {newChunks.Count} new chunks from selected document pages");

                var fileTopics = getTopicTagString(request.TopicTags);

                request.Metadata.Add(nameof(FileChunkMetadata.MODEL).ToLower(), request.EmbeddingModel ?? _settings.DefaultEmbedder);
                request.Metadata.Add(nameof(FileChunkMetadata.DIMENSIONS).ToLower(), request.Dimensions);
                request.Metadata.Add(nameof(FileChunkMetadata.FILE_EXTENSION).ToLower(), request.FileExtension);
                request.Metadata.Add(nameof(FileChunkMetadata.TOPICS).ToLower(), fileTopics);

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


                setCollectionMetadata(nameof(FileCollectionMetadata.FILES).ToLower(), $"{request.FileName}.{request.FileExtension}", $"{collection.GetMeta<FileCollectionMetadata>().FILES}", request);
                setCollectionMetadata(nameof(FileCollectionMetadata.CHUNK_SIZES).ToLower(), $"{request.ChunkSize}", $"{collection.GetMeta<FileCollectionMetadata>().CHUNK_SIZES}", request);
                setCollectionMetadata(nameof(FileCollectionMetadata.CHUNK_OVERLAPS).ToLower(), $"{request.ChunkOverlap}", $"{collection.GetMeta<FileCollectionMetadata>().CHUNK_OVERLAPS}", request);
                setCollectionMetadata(nameof(FileCollectionMetadata.SKIPPED_PAGES).ToLower(), $"{request.InitialSkip}", $"{collection.GetMeta<FileCollectionMetadata>().SKIPPED_PAGES}", request);
                setCollectionMetadata(nameof(FileCollectionMetadata.PAGE_CUTOFFS).ToLower(), $"{request.PageCutoff}", $"{collection.GetMeta<FileCollectionMetadata>().PAGE_CUTOFFS}", request);
                setCollectionMetadata(nameof(FileCollectionMetadata.TOPICS).ToLower(), fileTopics, $"{collection.GetMeta<FileCollectionMetadata>().TOPICS}", request);
                request.Metadata.Add(nameof(FileCollectionMetadata.PAGES).ToLower(), $"{document.Pages.Count}");

                _logger.LogInformation($"Inserted collection: {request.Name} >> Embedded file: {request.FileName}");
                
                return await _repo.UpdateCollectionData(request.Name, request.Metadata, isOverride: true);
            }
            catch(HttpException fileEx)
            {
                _logger.LogInformation($"{nameof(CreateCollectionFromFile)} >> {request.Name} >> {fileEx.Message}");
                throw fileEx;
            }
            catch(Exception ex)
            {
                if(ex.GetType() != typeof(HttpException))
                    await _repo.DeleteCollection(request.Name);

                throw ex;
                //throw new HttpException($"An error has occured while creating the collection {request.Name} >> {ex.GetType().Name}: {ex.Message}");


            }
        }
        public async Task<ChromaFilesCollection> CreateEmptyFileCollection(CreateCollectionRequest request)
        {
            await _repo.CreateCollection(request.Name, request.Description ?? request.Name, request.EmbeddingModel, request.Dimensions);

            return await _repo.GetCollection(request.Name);
        }
        public async Task<ChromaFilesCollection> InspectCollection(string name, int startIndex = 0, int samples = 0, bool includeEmbeddings = false)
        {
            var collection = await _repo.GetCollection(name);

            var ids = new List<string>();

            if (startIndex > collection.GetMeta<FileCollectionMetadata>().TOTAL_CHUNKS)
                throw new InvalidOperationException($"The requested chunk samples start index is grater or equal to the number of available chunks ({collection.GetMeta<FileCollectionMetadata>().TOTAL_CHUNKS})");

            if (startIndex < 1)
                startIndex = 1;

            if (samples == 0 || samples > collection.GetMeta<FileCollectionMetadata>().TOTAL_CHUNKS)
                samples = collection.GetMeta<FileCollectionMetadata>().TOTAL_CHUNKS;

            for (int i = startIndex; i < startIndex + samples; i++)
                if (i <= collection.GetMeta<FileCollectionMetadata>().TOTAL_CHUNKS)
                    ids.Add($"{i}");

            return await _repo.InspectCollection(name, ids, includeEmbeddings);
        }
        private void setCollectionMetadata(string key, string newValue, string currentValue, EmbedCollectionRequest request)
        {
            if (string.IsNullOrEmpty(newValue)) return;

            string value = string.IsNullOrEmpty(currentValue.Trim()) ? newValue : $"{currentValue},{newValue}";

            if (request.Metadata.ContainsKey(key))
                request.Metadata[key] = value;

            else request.Metadata.Add(key, value);

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

            return await _repo.InsertChunks(request.Name, chunks, extraMetas: request.Metadata);
        }

        private string getPreviousChunkOverlap(ChromaChunk prevChunk, int overlappedTokens)
        {
            var text = prevChunk.Text.Replace("\n", "").Replace("\\", "");
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
            var text = nextChunk.Text.Replace("\n", "").Replace("\\", "");
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
        private List<ChromaChunk> phunkPages(List<DocumentPage> pages, string docName, int chunkSize)
        {
            var chunks = new List<ChromaChunk>();
            var pagesIdx = new List<int>();
            
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
                        var sentence = sentences[i].Trim().Replace("\n", "").Replace("\\", "");
                        
                        if (string.IsNullOrEmpty(sentence)) continue;

                        var tokens = sentence.Split(" ").ToList();

                        for(int j = 0; j < tokens.Count; j++)
                        {
                            if (tokens[j].StartsWith("\\")) continue;

                            if (currentChunkSize >= chunkLength)
                            {
                                var metadata = new Dictionary<string, object>();
                                metadata.Add(nameof(FileChunkMetadata.DOCUMENT_NAME).ToLower(), docName);
                                var chunkedPagesIds = "";
                                pagesIdx.ForEach(idx => {
                                    chunkedPagesIds += $"{idx},";
                                    metadata.Add($"page_{idx}", true);
                                });
                                metadata.Add(nameof(FileChunkMetadata.PAGES).ToLower(), chunkedPagesIds.Substring(0, chunkedPagesIds.Length -1));
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
                var metadata = new Dictionary<string, object>();
                metadata.Add(nameof(FileChunkMetadata.DOCUMENT_NAME).ToLower(), docName);
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
                metadata.Add(nameof(FileChunkMetadata.PAGES).ToLower(), chunkedPagesIds.Substring(0, chunkedPagesIds.Length -1));
                chunks.Add(new ChromaChunk { Text = sb.ToString(), Metadata = metadata });
            }

            return chunks;
        }
       
        private List<ChromaChunk> splitAndChunk(DocumentPage page, string docName,  int chunkSize)
        {
            var chunks = new List<ChromaChunk>();

            int totalNewChunks = (int)Math.Ceiling((decimal)((float)page.Text.Length / (float)chunkSize));
            float chunkLength = page.Text.Length / totalNewChunks;
            int currentChunkSize = 0;
            var chunkBuilder = new StringBuilder();
            var currentToken = "";
            page.Text.Split(".").ToList().ForEach(sentence => 
            {
                if(!string.IsNullOrEmpty(sentence.Trim()))
                {
                    sentence.Trim()
                    .Replace("\n", "")
                    .Replace("\\", "")
                    .Split(" ")
                    .ToList().ForEach(token => {
                        currentToken = $" {token}";
                        currentChunkSize += currentToken.Length;
                        chunkBuilder.Append(currentToken);
                        if (currentChunkSize >= chunkLength)
                        {
                            var metadata = new Dictionary<string, object>();
                            metadata.Add(nameof(FileChunkMetadata.DOCUMENT_NAME).ToLower(), docName);
                            metadata.Add(nameof(FileChunkMetadata.PAGES).ToLower(), $"{page.PageNumber}");
                            metadata.Add($"page_{page.PageNumber}", true);
                            chunks.Add(new ChromaChunk { Text = chunkBuilder.ToString().Trim(), Metadata = metadata });
                            currentChunkSize = 0;
                            chunkBuilder = new StringBuilder();
                        }
                    });
                    if (currentChunkSize < chunkLength)
                        chunkBuilder.Append(".");
                }
            });
            if (currentChunkSize > 0)
            {
                var metadata = new Dictionary<string, object>();
                metadata.Add(nameof(FileChunkMetadata.DOCUMENT_NAME).ToLower(), docName);
                metadata.Add(nameof(FileChunkMetadata.PAGES).ToLower(), $"{page.PageNumber}");
                metadata.Add($"page_{page.PageNumber}", true);
                chunks.Add(new ChromaChunk { Text = chunkBuilder.ToString().Trim(), Metadata = metadata });
            }
            return chunks;
        }

        private string getTopicTagString(List<string> tags)
        {
            StringBuilder sb = new StringBuilder();

            tags.ForEach(tag => sb.Append(tag).Append(","));

            var result = sb.ToString();

            return result.Substring(0, result.Length -1);
        }

        public async Task<List<ChromaFilesCollection>> GetAllCollections()
            => await _repo.CollectionsOf(EChunkType.FILE);
    }
}
