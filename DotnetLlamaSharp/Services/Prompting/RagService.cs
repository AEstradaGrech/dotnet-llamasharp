using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Domain.Services.Prompting;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using OllamaSharp.Models.Chat;
using System.Text;
using System.Text.Json;

namespace DotnetLlamaSharp.Services.Prompting
{
    public class RagService : IRagService
    {
        private readonly IOllamaSharpService _chatService;
        private readonly IChromaService _chromaService;
        private readonly IEmbeddingsService _embeddingsService;
        private readonly IChromaChatsRepository _chatsRepo;
        private readonly ILogger<RagService> _logger;
        private readonly IMapper _mapper;
        private readonly ApiSettings _settings;

        public RagService(IOllamaSharpService chatService, IChromaService chromaService, IChromaChatsRepository chatsRepo,
            IEmbeddingsService embeddingsService, ILogger<RagService> logger, IMapper mapper, IOptions<ApiSettings> settings)
        {
            _chatService = chatService;
            _chromaService = chromaService;
            _logger = logger;
            _mapper = mapper;
            _chatsRepo = chatsRepo;
            _settings = settings.Value;
            _embeddingsService = embeddingsService;
        }

        public async Task<RagPrompt> SimpleRagQuery(RagPromptRequest request)
        {
            ChromaQuery chromaQuery = null;
            var collectionQueries = new Dictionary<string, List<ChromaQueryChunk>>();
            
            if (request.QueryCollections.Count > 0)
                collectionQueries = await QueryCollections(request.QueryCollections, request.Prompt, request.CollectionRetrievals, request.MinDistance, request.EmbeddingFilters);

            var baseIntruction = request.SystemMessage.Trim();

            var systemChunk = await _chromaService.GetSysMessage("system-messages", "rag-query");
            var systemMessage = @$"
{(string.IsNullOrEmpty(baseIntruction) ? "You are a helpful assistant." : baseIntruction)}

{systemChunk.Text}

{getFileRagStringResult(collectionQueries)}";
        
            var chatRequest = _mapper.Map<RagPromptRequest, SimplePromptRequest>(request);

            chatRequest.SystemMessage = systemMessage;
      
            var response = await _chatService.SimplePrompt(chatRequest);
            
            return new RagPrompt
            {
                Model = response.Model,
                EmbeddingModel = chromaQuery != null ? chromaQuery.EmbeddingModel : null,
                Input = request.Prompt,
                Output = response.Output,
                IncludedChunks = collectionQueries.SelectMany(kvp => kvp.Value).ToList(),
                ChatHistory = response.ChatHistory,
                InputEmbedding = chromaQuery != null ? chromaQuery.QueryEmbedding : null
            };
        }

        public async Task<ChatPrompt> RagChatPrompt(RagChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Prompt))
                throw new BadHttpRequestException("No prompt found. Type something to start chatting");

            if (string.IsNullOrEmpty(request.Model))
                request.Model = _settings.DefaultModel;

            if (string.IsNullOrEmpty(request.AgentName))
                request.AgentName = request.Model;

            if (string.IsNullOrEmpty(request.AgentName.Trim()))
                throw new BadHttpRequestException($"No Agent Name has been set neither a llm model has been found to use in its place");

            if (string.IsNullOrEmpty(request.UserName.Trim()))
                request.UserName = _settings.DefaultUserName;

            if (string.IsNullOrEmpty(request.UserName))
                throw new BadHttpRequestException("No user name has been found in the request and appsetings");

            if (string.IsNullOrEmpty(request.SystemMessage))
                request.SystemMessage = $"You are a helpful assistant named: {request.AgentName}. Your task is to chat with the user, {request.UserName}.";

            else request.SystemMessage += $"\n\nYour name is {request.AgentName}. You are starting a conversation with the user, {request.UserName}.";
            
            var collectionName = $"{request.AgentName.Trim().Replace(" ", "")}-{request.UserName.Trim().Replace(" ", "")}";
            
            var isFirstSession = !await _chatsRepo.CollectionExists(collectionName);

            ChromaChatsCollection collection = null;
            if (isFirstSession)    
                collection = await _chatsRepo.InitCollection(request.AgentName, request.UserName, _settings.DefaultEmbedder, _settings.DefaultDimensions);
            
            else collection = await _chatsRepo.GetCollection(collectionName);

            var sessionChunk = isFirstSession ? await _chatsRepo.DefaultChunk(collection.Name) : await _chatsRepo.GetChunkById(collection.Name, collection.GetMeta<ChatCollectionMetadata>().CURRENT_SESSION_ID);

            ChromaChatChunk currentChunk = null;
            if (isFirstSession || request.IsNewSession)
            {
                var initTemplate = await _chromaService.GetSysMessage("system-messages", "rag-chat-init");

                if (string.IsNullOrEmpty(initTemplate.Text))
                    throw new InvalidDataException($"{nameof(RagChatPrompt)} >> collection {collection.Name} >> No system INIT MESSAGE found");

                string ragChatTemplate = $"{request.SystemMessage.Trim()} {initTemplate.Text}";

                sessionChunk.AppendMessage(ChatRole.System, ragChatTemplate);

                sessionChunk.UpdateType(EChunkType.SESSION);

                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES).ToLower(), 1);
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.CHAT_INIT).ToLower(), true);
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.CURRENT).ToLower(), true);
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.LLM_MODEL).ToLower(), request.Model, resetDefault: true);

                currentChunk = await generateNextChunk(sessionChunk, sessionChunk, collection, [new ChatMessage(ChatRole.User.ToString(), request.Prompt)], isSessionInit: true);

                collection = await _chatsRepo.GetCollection(collection.Name);

                sessionChunk = await _chatsRepo.GetChunkById(collection.Name, collection.GetMeta<ChatCollectionMetadata>().CURRENT_SESSION_ID);
            }
            else 
            {
                var filterMetas = new Dictionary<string, object>();
                
                filterMetas.Add(nameof(ChatChunkMetadata.CURRENT).ToLower(), true);
                filterMetas.Add(nameof(ChatChunkMetadata.SESSION_ID).ToLower(), collection.GetMeta<ChatCollectionMetadata>().CURRENT_SESSION_ID);

                var chunks = await _chatsRepo.GetChunks(collectionName, filterMetas,withEmbeddings: false);
                
                if (!chunks.Any())
                    throw new InvalidDataException($"No CURRENT chunk has been found for collection: {collectionName} | session chunk: {sessionChunk.Id}");

                currentChunk = chunks.First();
                currentChunk.AppendMessage(ChatRole.User.ToString(), request.Prompt);
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES).ToLower(), sessionChunk.GetMeta<ChatChunkMetadata>().TOTAL_MESSAGES + 1, resetDefault: true);

            }
            var messages = currentChunk.TextAsMessages();

            var systemMessage = sessionChunk.TextAsMessages().FirstOrDefault(); 
            // SYSTEM MESSAGE / RAG stuff
            var memoBuilder = new StringBuilder();
            if (currentChunk.GetMeta<ChatChunkMetadata>().SESSION_CHUNKS > 2)
            {
                var chatMemos = new List<ChromaQueryChunk>();

                var currentEmbedding = await _embeddingsService.GenerateEmbeddings(currentChunk.Text, currentChunk.DefaultMetadata.DIMENSIONS, currentChunk.DefaultMetadata.MODEL);

                var memoFilters = new Dictionary<string, object>();
                memoFilters.Add(nameof(ChatChunkMetadata.SESSION_ID).ToLower(), sessionChunk.Id);
                memoFilters.Add(nameof(ChatChunkMetadata.CURRENT).ToLower(), false);
                memoFilters.Add(nameof(ChatChunkMetadata.CHAT_INIT).ToLower(), false);

                var sessionEmbeddings = await _chatsRepo.QueryCollection(collection.Name, currentEmbedding.GeneratedEmbeddings.FirstOrDefault().Vector, _settings.RagChatMemoryRetrievals, memoFilters);

                chatMemos = _settings.RagChatMemoMinDistance <= 0 ? sessionEmbeddings : sessionEmbeddings.Where(x => x.Distance <= _settings.RagChatMemoMinDistance).ToList();

                for (int i = 0; i < chatMemos.Count; i++)
                    memoBuilder.Append($"\n\n- CHAT MEMORY FRAGMENT #{i}:\n")
                               .Append(chatMemos[i]
                                .As<ChromaChatChunk>()
                                .EmbeddedText(collection.AgentName, collection.UserName));
            }

            systemMessage.Content = systemMessage.Content.Replace("[[-CHAT_MEMORIES-]]", currentChunk.GetMeta<ChatChunkMetadata>().SESSION_CHUNKS > 1 ? memoBuilder.ToString().Trim() : "(No memories yet for this conversation)");

            string? ragData = null;
            if(request.QueryCollections.Any())
                ragData = await QueryCollections(request.Prompt, request.QueryCollections, request.CollectionRetrievals, request.MinDistance, request.EmbeddingFilters);

            systemMessage.Content = systemMessage.Content.Replace("[[-RETRIEVED_DATA-]]", string.IsNullOrEmpty(ragData) ? "(Nothing relevant...)" : ragData);

            request.ChatHistory = [systemMessage];

            if(currentChunk.Id == sessionChunk.Id)
                request.ChatHistory.AddRange(messages.Skip(1).Take(messages.Count -1).ToList());
            
            else request.ChatHistory.AddRange(messages); 

            if (request.ChatHistory.Last().Role == ChatRole.User.ToString())
                request.ChatHistory = request.ChatHistory.SkipLast(1).ToList();

            var response = await _chatService.ChatPrompt(_mapper.Map<RagChatRequest, ChatPromptRequest>(request));

            currentChunk.AppendMessage(ChatRole.Assistant.ToString(), response.Output);

            currentChunk = await _chatsRepo.UpsertChunk(collectionName, currentChunk);

            //TotalMessages does not count original Session message (with instrucition message)
            sessionChunk.AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES).ToLower(), sessionChunk.GetMeta<ChatChunkMetadata>().TOTAL_MESSAGES + 1);

            sessionChunk = await _chatsRepo.UpsertChunk(collection.Name, sessionChunk);
            
            if (currentChunk.Text.Length >= _settings.RagChatChunkSize)
            {
                var overlaps = new List<ChatMessage> { new ChatMessage(ChatRole.User.ToString(), response.Input) };
                overlaps.Add(new ChatMessage(ChatRole.Assistant.ToString(), response.Output));
                var nextChunk = await generateNextChunk(currentChunk, sessionChunk, collection, overlaps);
            }

            return response;
        }

        public async Task<Dictionary<string, List<ChromaQueryChunk>>> QueryCollections(List<string> names, string text, int resultsNumber, double? minDistance, Dictionary<string, object> filters)
        {
            if (minDistance == 0)
                minDistance = null;

            var results = new Dictionary<string, List<ChromaQueryChunk>>();
            ChromaQuery queryResult = null;
            foreach (var name in names)
            {
                results.Add(name, new List<ChromaQueryChunk>());
                queryResult = await _chromaService.QueryCollection(name, text, resultsNumber, filters);

                foreach (var chunk in queryResult.Chunks)
                {
                    if (minDistance != null && chunk.Distance > minDistance) break;

                    results[name].Add(chunk);
                }
            }

            return results;
        }

        public async Task<string> QueryCollections(string text, List<string> names, int resultsNumber, double? minDistance, Dictionary<string, object> filters)
            => getFileRagStringResult(await QueryCollections(names, text, resultsNumber, minDistance, filters));

        private async Task<ChromaChatChunk> generateNextChunk(ChromaChatChunk currentChunk, ChromaChatChunk sessionChunk, ChromaChatsCollection collection, List<ChatMessage> overlaps, bool isSessionInit = false)
        {
            var embeddings = await _embeddingsService.GenerateEmbeddings(currentChunk.EmbeddedText(collection.AgentName, collection.UserName), collection.DefaultMetadata.DIMENSIONS, collection.DefaultMetadata.MODEL);

            if (!embeddings.GeneratedEmbeddings.Any())
                throw new ArgumentNullException($"An error has occured while generating the embeddings for chat collection: {collection.Name} >> CHUNK: {currentChunk.Id} >> No generated embeddings");

            currentChunk.Embedding = embeddings.GeneratedEmbeddings.First().Vector;
            currentChunk.AddMetadata(nameof(ChatChunkMetadata.CURRENT).ToLower(), false);

            currentChunk = await _chatsRepo.UpsertChunk(collection.Name, currentChunk);

            if(isSessionInit)
            {
                sessionChunk = currentChunk;
                currentChunk.AddMetadata(nameof(ChatChunkMetadata.SESSION_ID).ToLower(), currentChunk.Id);
                currentChunk.AddMetadata(nameof(ChatChunkMetadata.SESSION_CHUNKS).ToLower(), 1, resetDefault:true);
            

                collection.AddMetadata(nameof(ChatCollectionMetadata.SESSION_IDS).ToLower(), string.IsNullOrEmpty(collection.GetMeta<ChatCollectionMetadata>().SESSION_IDS) ? currentChunk.Id : $"{collection.GetMeta<ChatCollectionMetadata>().SESSION_IDS},{currentChunk.Id}");
                collection.AddMetadata(nameof(ChatCollectionMetadata.CURRENT_SESSION_ID).ToLower(), currentChunk.Id);
                collection.AddMetadata(nameof(ChatCollectionMetadata.TOTAL_SESSIONS).ToLower(), collection.GetMeta<ChatCollectionMetadata>().TOTAL_SESSIONS + 1);
                collection.AddMetadata(nameof(ChatCollectionMetadata.CURRENT_SESSION_CHUNKS).ToLower(), 1);
                collection.AddMetadata(nameof(ChatCollectionMetadata.TOTAL_CHUNKS).ToLower(), collection.GetMeta<ChatCollectionMetadata>().TOTAL_CHUNKS + 1, resetDefault: true);
            }

            var nextChunk = _chatsRepo.DefaultChunk(currentChunk.Metadata);
            nextChunk.SetEmpty(isCurrent: true);
            nextChunk.UpdateType(EChunkType.CHAT);
            //Both the last chunk and the first one (sessionChunk) track the total session chunks
            nextChunk.AddMetadata(nameof(ChatChunkMetadata.SESSION_CHUNKS).ToLower(), currentChunk.GetMeta<ChatChunkMetadata>().SESSION_CHUNKS + 1);

            if (overlaps.Count > 0)
                overlaps.ForEach(message => nextChunk.AppendMessage(new ChatRole(message.Role), message.Content));

            var nextChunkEmbeddings = await _embeddingsService.GenerateEmbeddings(nextChunk.EmbeddedText(collection.AgentName, collection.UserName), nextChunk.DefaultMetadata.DIMENSIONS, nextChunk.DefaultMetadata.MODEL);

            if (!nextChunkEmbeddings.GeneratedEmbeddings.Any())
                throw new InvalidDataException($"An error has occured while generating the chunk overlap for the next chunk >> NO EMBEDDINGS >> collection: {collection.Name} >> current chunk ID: {currentChunk.Id}");

            nextChunk.Embedding = nextChunkEmbeddings.GeneratedEmbeddings.First().Vector;

            nextChunk = await _chatsRepo.InsertChunk(collection.Name, nextChunk);

            sessionChunk.AddMetadata(nameof(ChatChunkMetadata.SESSION_CHUNKS).ToLower(), sessionChunk.GetMeta<ChatChunkMetadata>().SESSION_CHUNKS + 1);

            sessionChunk = await _chatsRepo.UpsertChunk(collection.Name, sessionChunk);

            collection.AddMetadata(nameof(ChatCollectionMetadata.TOTAL_CHUNKS).ToLower(), collection.GetMeta<ChatCollectionMetadata>().TOTAL_CHUNKS + 1);
            collection.AddMetadata(nameof(ChatCollectionMetadata.CURRENT_SESSION_CHUNKS).ToLower(), sessionChunk.GetMeta<ChatChunkMetadata>().SESSION_CHUNKS);

            await _chatsRepo.UpdateCollectionData(collection.Name, collection.Metadata);

            return nextChunk;
        }

        private string getFileRagStringResult(Dictionary<string, List<ChromaQueryChunk>> collectionQueries)
        {
            var sb = new StringBuilder().Append("### RETRIEVED DATA:\n");

            collectionQueries.Keys.ToList()
                .ForEach(key => collectionQueries[key]
                    .OrderBy(chunk => chunk.Distance)
                    .ToList()
                    .ForEach(chunk =>
                        sb.Append($"\n- Topic: ")
                            .Append(key)
                            .Append("\n- Source document: ")
                            .Append(chunk.DefaultMetadata.DOCUMENT_NAME)
                            .Append("\n- Data:\n\n")
                            .Append(chunk.Text)
                            .Append("\n")));

            return sb.ToString().Trim();
        }

        public async Task<RagPrompt> YesNoQuestion(RagPromptRequest request)
        {
            var systemMessage = $"{request.SystemMessage.Trim()}";
            ChromaQuery chromaQuery = null;
            var collectionQueries = new Dictionary<string, List<ChromaQueryChunk>>();

            if (request.QueryCollections.Count > 0)
            {
                collectionQueries = await QueryCollections(request.QueryCollections, request.Prompt, request.CollectionRetrievals, request.MinDistance, request.EmbeddingFilters);
                systemMessage += $"{getFileRagStringResult(collectionQueries)}";
            }

            request.SystemMessage = systemMessage;

            var response = await _chatService.BooleanQuestion(_mapper.Map<RagPromptRequest, SimplePromptRequest>(request));

            return new RagPrompt
            {
                Model = response.Model,
                EmbeddingModel = chromaQuery != null ? chromaQuery.EmbeddingModel : null,
                Input = request.Prompt,
                Output = response.Output,
                IncludedChunks = collectionQueries.SelectMany(kvp => kvp.Value).ToList(),
                ChatHistory = response.ChatHistory,
                InputEmbedding = chromaQuery != null ? chromaQuery.QueryEmbedding : null
            };
        }
    }
}
