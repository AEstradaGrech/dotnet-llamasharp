using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
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
            var chunksCollection = new Dictionary<string, string>();
            var includedChunks = new List<ChromaQueryChunk>();
            if(request.QueryCollections.Count > 0)
            {
                foreach(var name in request.QueryCollections)
                {
                    chromaQuery = await _chromaService.QueryCollection(name, request.Prompt, request.CollectionRetrievals, request.EmbeddingFilters);

                    foreach(var chunk in chromaQuery.Chunks)
                    {
                        if (request.MinDistance != null && chunk.Distance > request.MinDistance) break;
                        
                        includedChunks.Add(chunk);
                        chunksCollection.Add(chunk.Id, name);
                    };
                }
            }

            var sb = new StringBuilder();

            includedChunks
                .OrderBy(chunk => chunk.Distance)
                .ToList()
                .ForEach(chunk =>
                    sb.Append($"\n- Topic: ")
                      .Append(chunksCollection[chunk.Id])
                      .Append("\n- Source document: ")
                      .Append(chunk.DefaultMetadata.DOCUMENT)
                      .Append("\n- Data:\n\n")
                      .Append(chunk.DefaultMetadata.TEXT)
                      .Append("\n"));

            var baseIntruction = request.SystemMessage.Trim();

            var systemMessage = @$"{(string.IsNullOrEmpty(baseIntruction) ? "You are a helpful assistant." : baseIntruction)}
            Your task is to answer any user query using the RETRIEVED_DATA as a grounded source of truth.
            
            Each item in the retrieve data section has three sections that will help you to understand the source and topic
            of the data along with the relevant document data that you must use to generate your response.

            Use the retrieved information to answer the user questions only if the question is related to
            the retrieved data topic.

            ### IMPORTANT: follow this STEPS in order to generate your response:

            # STEP 1: Analyze the content of the user query to get an idea of the topic he is querying about.
            # STEP 2: Review the RETRIEVED DATA section and try to find relevant data to use as source to generate your response (the 'Topic' and 'Source' values of 
                      each data item will help you to do that).
            # STEP 3: Use your reasonaments from STEP 1 and STEP 2 to decide if the RETRIEVED DATA is meaningful in order to generate your response according to
                      the queried topic.
            # STEP 4: Based on your analysis from STEP 3 generate a coherent response using the RETRIEVED DATA to support your answer if you have decided that it
                      is aligned with the general topic of the user question. In case you have decided that the user query is unrelated to the retrived data, 
                      ignore the RETRIEVED DATA and try to answer the anyways but always informing the user that you have not found any relevant data in your Knowledge Base about the queried topic.

            ### RETRIEVED DATA:

            {sb.ToString().Trim()}";
        
            var chatRequest = _mapper.Map<RagPromptRequest, ChatPromptRequest>(request);
            chatRequest.SystemMessage = systemMessage;
      
            var response = await _chatService.SimplePrompt(chatRequest);
            
            return new RagPrompt
            {
                Model = response.Model,
                EmbeddingModel = chromaQuery != null ? chromaQuery.EmbeddingModel : null,
                Input = request.Prompt,
                Output = response.Output,
                IncludedChunks = includedChunks,
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

            if (string.IsNullOrEmpty(request.AgentName))
                throw new BadHttpRequestException($"No Agent Name has been set neither a llm model has been found to use in its place");

            if (string.IsNullOrEmpty(request.UserName))
                request.UserName = _settings.DefaultUserName;

            if (string.IsNullOrEmpty(request.UserName))
                throw new BadHttpRequestException("No user name has been found in the request and appsetings");

            if (string.IsNullOrEmpty(request.SystemMessage))
                request.SystemMessage = $"You are a helpful assistant named: {request.AgentName}. Your task is to chat with the user, {request.UserName}";

            else request.SystemMessage += $"\n\nYour name is {request.AgentName}. The user is named: {request.UserName}";
            
            var collectionName = $"{request.AgentName}-{request.UserName}";
            
            var isFirstSession = await _chatsRepo.CollectionExists(collectionName);

            ChromaChatCollection collection = isFirstSession ? 
                await _chatsRepo.CreateCollection(collectionName, description: $"Ollama Rag Chat >> {request.AgentName} - {request.UserName} >> {DateTime.Now}", _settings.DefaultEmbedder, 512) :
                await _chatsRepo.GetCollection(collectionName);

            var sessionChunk = isFirstSession ? await _chatsRepo.DefaultChunk(collection.Name) : await _chatsRepo.GetChunkById(collection.Name, "1");

            ChromaChatChunk currentChunk = null;
            if (isFirstSession || request.IsNewSession)
            {
                var sessionMetas = new Dictionary<string, object>();
                
                sessionMetas.Add(nameof(ChatChunkMetadata.TOTAL_MESSAGES).ToLower(), 2);
                sessionMetas.Add(nameof(ChatChunkMetadata.CHAT_INIT).ToLower(), true);
                sessionMetas.Add(nameof(ChatChunkMetadata.CURRENT).ToLower(), true);
                sessionMetas.Add(nameof(ChatChunkMetadata.LLM_MODEL).ToLower(), request.Model);
                
                
                
                sessionChunk.AppendMessage(ChatRole.System, request.SystemMessage);
                sessionChunk.AppendMessage(ChatRole.User, request.Prompt);

                currentChunk = await _chatsRepo.UpsertChunk(collectionName, sessionChunk, bAddTextAsMeta: true, sessionMetas);

                collection.AddMetadata(nameof(ChatCollectionMetadata.SESSION_IDS).ToLower(), isFirstSession ? currentChunk.Id : $"{collection.DefaultMetadata.SESSION_IDS},{currentChunk.Id}");
                collection.AddMetadata(nameof(ChatCollectionMetadata.CURRENT_SESSION_ID).ToLower(), currentChunk.Id);
                collection.AddMetadata(nameof(ChatCollectionMetadata.TOTAL_SESSIONS).ToLower(), isFirstSession ? 0 : collection.DefaultMetadata.TOTAL_SESSIONS + 1);
                collection.AddMetadata(nameof(ChatCollectionMetadata.CURRENT_SESSION_CHUNKS).ToLower(), 0);

                await _chatsRepo.UpdateCollectionData(collectionName, collection.Metadata);
            }
            else 
            {
                currentChunk = await _chatsRepo.GetChunkById(collectionName, collection.DefaultMetadata.CURRENT_SESSION_ID);
                currentChunk.AppendMessage(ChatRole.User.ToString(), request.Prompt);
            }
            var messages = currentChunk.TextAsMessages();

            // SYSTEM MESSAGE / RAG stuff
            
            request.ChatHistory = messages;

            var response = await _chatService.ChatPrompt(request);

            currentChunk.AppendMessage(ChatRole.Assistant.ToString(), response.Output);

            await _chatsRepo.UpsertChunk(collectionName, currentChunk);

            if (currentChunk.Text.Length >= _settings.RagChatChunkSize)
            {
                var embeddings = await _embeddingsService.GenerateEmbeddings(currentChunk.Text, collection.DefaultMetadata.DIMENSIONS, collection.DefaultMetadata.MODEL);

                if (!embeddings.GeneratedEmbeddings.Any())
                    throw new ArgumentNullException($"An error has occured while generating the embeddings for chat collection: {collection.Name} >> CHUNK: {currentChunk.Id} >> No generated embeddings");

                currentChunk.Embedding = embeddings.GeneratedEmbeddings.First().Vector;
                currentChunk.AddMetadata(nameof(ChatChunkMetadata.CURRENT).ToLower(), false);

                await _chatsRepo.UpsertChunk(collection.Name, currentChunk);

                var nextChunk = await _chatsRepo.DefaultChunk(collection.Name, currentChunk.Metadata);
                
                nextChunk.AddMetadata(nameof(ChatChunkMetadata.CURRENT).ToLower(), true);
                nextChunk.AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES).ToLower(), 0, resetDefault: true);

                nextChunk.AppendMessage(ChatRole.User, response.Input);
                nextChunk.AppendMessage(ChatRole.Assistant, response.Output);

                await _chatsRepo.UpsertChunk(collection.Name, nextChunk);

                collection.AddMetadata(nameof(ChatCollectionMetadata.CHUNKS).ToLower(), collection.DefaultMetadata.CHUNKS + 1);

                await _chatsRepo.UpdateCollectionData(collection.Name, collection.Metadata);
            }

            sessionChunk.AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES), sessionChunk.DefaultMetadata.TOTAL_MESSAGES + 2);

            await _chatsRepo.UpsertChunk(collection.Name, sessionChunk); 
            
            return response;
        }
    }
}
