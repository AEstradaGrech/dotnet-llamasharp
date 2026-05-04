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

            var systemMessage = @$"
{(string.IsNullOrEmpty(baseIntruction) ? "You are a helpful assistant." : baseIntruction)}
Your task is to chat with the user and answer any question using the RETRIEVED_DATA (if any) as a grounded source of truth.
            
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
# STEP 5: Use the result of your analisys from the previous steps to generate answer the user in a natural / conversational way and accordingly to any provided role or character profile.

{getFileRagStringResult(collectionQueries)}";
        
            var chatRequest = _mapper.Map<RagPromptRequest, ChatPromptRequest>(request);
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

            ChromaChatCollection collection = null;
            if (isFirstSession)    
                collection = await _chatsRepo.InitCollection(request.AgentName, request.UserName, _settings.DefaultEmbedder, _settings.DefaultDimensions);
            
            else collection = await _chatsRepo.GetCollection(collectionName);

            var sessionChunk = isFirstSession ? await _chatsRepo.DefaultChunk(_settings.DefaultEmbedder, _settings.DefaultDimensions) : await _chatsRepo.GetChunkById(collection.Name, "1");

            ChromaChatChunk currentChunk = null;
            if (isFirstSession || request.IsNewSession)
            {
                string ragChatTemplate = $@"
{request.SystemMessage}

Your task is to chat with the user according to the stated instructions if any.

#IMPORTANT: follow this steps in order to generate your response:
    > STEP 1: Analyze any privided information about your character in terms of personality, psychology and/or attitude and 
              figure out a character that aligns with the provided information to adapt your response according to that character profile.
    > STEP 2: Review the CHAT MEMORIES section containing past fragments of conversation to get a better idea of your relationship with the user,
              the context of the conversation and part of the conversation that might be related to the current topic.
    > STEP 3: Review the RETRIEVED DATA section containing pieces of information that might br related to the current user prompt (or not). Use the
              'topic' and 'source' labels of each fragment to determine if the information is within the scope of the current user query and, in case
               you find it is useful, use it to generate a more rich and accurate response.
    > STEP 4: Think up a response for the user that aligns with any provided character profile and is consistent with the overall converation and
              provided sources of data.
    > STEP 5: Review carefully the RULES section and ENSURE your response is compliant with them and adapt to response
              as necesary before outputting your final response.

#RULES: take into account this rules before outputting your final response:
    - If you have been assigned a name, you MUST respond to that name.
    - If you have you have been informe of the user name, address the user using that name
    - If you have been passed any kind of character profile, role or psychological traits, always ENSURE
      that you act and respond according to it. Align your behaviour and mood with the provided profile. Never break the character.
    - USE the provided CHAT MEMORIES (if any) to have a better understanding of the context of the conversation.
    - USE the provided RETRIEVED DATA (if any) to support your anwers ONLY if their topic is within the scope of the user prompt. Otherwise, ignore that secion.

#CHAT MEMORIES: This is what you recall from previous part of the conversation. This memory fragments will give you a grounded source of truth regarding the conversation:

[[-CHAT_MEMORIES-]]

#RETRIEVED DATA: Use this pieces of information as a grounded source of truth to support your responses regarding specific topics. Ignore them if you find them irrelevant for the current chat turn.

[[-RETRIEVED_DATA-]]
";
                var sessionEmbedding = await _embeddingsService.GenerateEmbeddings(ragChatTemplate, collection.GetMeta<ChatCollectionMetadata>().DIMENSIONS, collection.GetMeta<ChatCollectionMetadata>().MODEL);

                if (!sessionEmbedding.GeneratedEmbeddings.Any())
                    throw new InvalidOperationException($"An error has occured while generating the session embedding for chat collection: {collection.Name}");
                sessionChunk.Embedding = sessionEmbedding.GeneratedEmbeddings.First().Vector;
                sessionChunk.AppendMessage(ChatRole.System, ragChatTemplate); // UserSysMessage + RagChatTemplate
                sessionChunk.AppendMessage(ChatRole.User, request.Prompt);

                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES).ToLower(), 2);
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.CHAT_INIT).ToLower(), true);
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.CURRENT).ToLower(), true);
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.LLM_MODEL).ToLower(), request.Model);
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.DOCUMENT).ToLower(), collection.Name);
                sessionChunk = await _chatsRepo.UpsertChunk(collectionName, sessionChunk, bAddTextAsMeta: true);
                currentChunk = sessionChunk;
                currentChunk.AddMetadata(nameof(ChatChunkMetadata.SESSION_ID).ToLower(), currentChunk.Id);
                currentChunk.AddMetadata(nameof(ChatChunkMetadata.SESSION_CHUNKS).ToLower(), 1);

                collection.AddMetadata(nameof(ChatCollectionMetadata.SESSION_IDS).ToLower(), isFirstSession ? currentChunk.Id : $"{collection.GetMeta<ChatCollectionMetadata>().SESSION_IDS},{currentChunk.Id}");
                collection.AddMetadata(nameof(ChatCollectionMetadata.CURRENT_SESSION_ID).ToLower(), currentChunk.Id);
                collection.AddMetadata(nameof(ChatCollectionMetadata.TOTAL_SESSIONS).ToLower(), isFirstSession ? 1 : collection.GetMeta<ChatCollectionMetadata>().TOTAL_SESSIONS + 1);
                collection.AddMetadata(nameof(ChatCollectionMetadata.CURRENT_SESSION_CHUNKS).ToLower(), 1);
                collection.AddMetadata(nameof(ChatCollectionMetadata.TOTAL_CHUNKS).ToLower(), collection.GetMeta<ChatCollectionMetadata>().TOTAL_CHUNKS + 1);
                collection = await _chatsRepo.UpdateCollectionData(collectionName, collection.Metadata);
            }
            else 
            {
                var filterMetas = new Dictionary<string, object>();
                filterMetas.Add(nameof(ChatChunkMetadata.CURRENT).ToLower(), true);
                //filterMetas.Add(nameof(ChatChunkMetadata.SESSION_ID).ToLower(), collection.GetMeta<ChatCollectionMetadata>().CURRENT_SESSION_ID);
                //var dummyEmbedding = await _embeddingsService.GenerateEmbeddings(sessionChunk.Text, currentChunk.DefaultMetadata.DIMENSIONS, currentChunk.DefaultMetadata.MODEL)
                var s = JsonSerializer.Serialize(sessionChunk.Embedding);
                var chunks = await _chatsRepo.QueryCollection(collectionName, sessionChunk.Embedding, 1, filterMetas);
                if (!chunks.Any())
                    throw new InvalidDataException($"No CURRENT chunk has been found for collection: {collectionName} | session chunk: {sessionChunk.Id}");
                currentChunk = _mapper.Map<ChromaQueryChunk, ChromaChatChunk>(chunks.First());
                currentChunk.AppendMessage(ChatRole.User.ToString(), request.Prompt);
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES).ToLower(), sessionChunk.GetMeta<ChatChunkMetadata>().TOTAL_MESSAGES + 1);

            }
            var messages = currentChunk.TextAsMessages();

            var systemMessage = sessionChunk.TextAsMessages().FirstOrDefault(); //Se guarda RagChatTemplate sin formatear. Se hace replace en cada request y se pasa como sysmesage
            // SYSTEM MESSAGE / RAG stuff
            var memoBuilder = new StringBuilder();
            if (currentChunk.GetMeta<ChatChunkMetadata>().SESSION_CHUNKS > 1)
            {
                var chatMemos = new List<ChromaQueryChunk>();

                var currentEmbedding = await _embeddingsService.GenerateEmbeddings(currentChunk.Text, currentChunk.DefaultMetadata.DIMENSIONS, currentChunk.DefaultMetadata.MODEL);

                var memoFilters = new Dictionary<string, object>();
                memoFilters.Add(nameof(ChatChunkMetadata.SESSION_ID).ToLower(), sessionChunk.Id);
                memoFilters.Add(nameof(ChatChunkMetadata.CURRENT).ToLower(), false);

                var sessionEmbeddings = await _chatsRepo.QueryCollection(collection.Name, currentEmbedding.GeneratedEmbeddings.FirstOrDefault().Vector, _settings.RagChatMemoryRetrievals, memoFilters);

                chatMemos = _settings.RagChatMemoMinDistance <= 0 ? sessionEmbeddings : sessionEmbeddings.Where(x => x.Distance <= _settings.RagChatMemoMinDistance).ToList();

                for (int i = 0; i < chatMemos.Count; i++)
                    memoBuilder.Append($"\n\n- CHAT MEMORY FRAGMENT #{i}:\n\n").Append(chatMemos[i].DefaultMetadata.TEXT);
            }

            systemMessage.Content = systemMessage.Content.Replace("[[-CHAT_MEMORIES-]]", currentChunk.GetMeta<ChatChunkMetadata>().SESSION_CHUNKS > 1 ? memoBuilder.ToString().Trim() : "(No memories yet for this conversation)");

            string? ragData = null;
            if(request.QueryCollections.Any())
                ragData = await QueryCollections(request.Prompt, request.QueryCollections, request.CollectionRetrievals, request.MinDistance, request.EmbeddingFilters);

            systemMessage.Content = systemMessage.Content.Replace("[[-RETRIEVED_DATA-]]", string.IsNullOrEmpty(ragData) ? "(Nothing relevant...)" : ragData);

            // getSessionSysMsg -> GetChunkById(1).TextAsMessages().First();
            // if currentChunk.Id > 1 <-- se puede hacer vector search
            // si collection.TotalSession > 1 <-- se puede hace vector search por sessions
            // si request.QueryCollections <-- se puede hacer vector search por files

            request.ChatHistory = [systemMessage]; //PerfilBot + TemplateMsg

            if(currentChunk.Id == sessionChunk.Id)
                request.ChatHistory.AddRange(messages.Skip(1).Take(messages.Count -1).ToList());
            
            else request.ChatHistory.AddRange(messages); // currentChunk stuff

            if (request.ChatHistory.Last().Role == ChatRole.User.ToString())
                request.ChatHistory = request.ChatHistory.SkipLast(1).ToList();

            var response = await _chatService.ChatPrompt(request);

            currentChunk.AppendMessage(ChatRole.Assistant.ToString(), response.Output);
            
            if(currentChunk != sessionChunk)
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES).ToLower(), sessionChunk.GetMeta<ChatChunkMetadata>().TOTAL_MESSAGES + 1);

            currentChunk = await _chatsRepo.UpsertChunk(collectionName, currentChunk);

            if (currentChunk.Text.Length >= _settings.RagChatChunkSize)
            {
                var embeddings = await _embeddingsService.GenerateEmbeddings(currentChunk.EmbeddedText, collection.DefaultMetadata.DIMENSIONS, collection.DefaultMetadata.MODEL);

                if (!embeddings.GeneratedEmbeddings.Any())
                    throw new ArgumentNullException($"An error has occured while generating the embeddings for chat collection: {collection.Name} >> CHUNK: {currentChunk.Id} >> No generated embeddings");

                var s = JsonSerializer.Serialize(embeddings.GeneratedEmbeddings.First().Vector);
                currentChunk.Embedding = embeddings.GeneratedEmbeddings.First().Vector;
                currentChunk.AddMetadata(nameof(ChatChunkMetadata.CURRENT).ToLower(), false);

                currentChunk = await _chatsRepo.UpsertChunk(collection.Name, currentChunk);

                var nextChunk = await _chatsRepo.DefaultChunk(currentChunk.Metadata);
                nextChunk.SetEmpty(isCurrent: true);

                //Both the last chunk and the first one (sessionChunk) track the total session chunks
                nextChunk.AddMetadata(nameof(ChatChunkMetadata.SESSION_CHUNKS).ToLower(), currentChunk.GetMeta<ChatChunkMetadata>().SESSION_CHUNKS + 1); 
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.SESSION_CHUNKS).ToLower(), sessionChunk.GetMeta<ChatChunkMetadata>().SESSION_CHUNKS + 1, resetDefault:true);

                nextChunk.AppendMessage(ChatRole.User, response.Input);
                nextChunk.AppendMessage(ChatRole.Assistant, response.Output);

                var nextChunkEmbeddings = await _embeddingsService.GenerateEmbeddings(nextChunk.EmbeddedText, nextChunk.GetMeta<ChromaMetadata>().DIMENSIONS, nextChunk.GetMeta<ChromaMetadata>().MODEL);

                if (!nextChunkEmbeddings.GeneratedEmbeddings.Any())
                    throw new InvalidDataException($"An error has occured while generating the chunk overlap for the next chunk >> NO EMBEDDINGS >> collection: {collection.Name} >> current chunk ID: {currentChunk.Id}");

                nextChunk.Embedding = nextChunkEmbeddings.GeneratedEmbeddings.First().Vector;

                nextChunk = await _chatsRepo.UpsertChunk(collection.Name, nextChunk);

                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.CURRENT).ToLower(), false);
                
                if (currentChunk.Id == sessionChunk.Id)
                    sessionChunk = await _chatsRepo.UpsertChunk(collection.Name, sessionChunk);

                collection.AddMetadata(nameof(ChatCollectionMetadata.TOTAL_CHUNKS).ToLower(), collection.GetMeta<ChatCollectionMetadata>().TOTAL_CHUNKS + 1);
                collection.AddMetadata(nameof(ChatCollectionMetadata.CURRENT_SESSION_CHUNKS).ToLower(), sessionChunk.GetMeta<ChatChunkMetadata>().SESSION_CHUNKS);

                await _chatsRepo.UpdateCollectionData(collection.Name, collection.Metadata);
            }

            if(currentChunk.Id != sessionChunk.Id)
                sessionChunk = await _chatsRepo.UpsertChunk(collection.Name, sessionChunk); 
            
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
                            .Append(chunk.DefaultMetadata.DOCUMENT)
                            .Append("\n- Data:\n\n")
                            .Append(chunk.DefaultMetadata.TEXT)
                            .Append("\n")));

            return sb.ToString().Trim();
        }
    }
}
