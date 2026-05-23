using AutoMapper;
using Dotnet.Chroma.Repositories.Models;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Core.Evaluators;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Core.TextGenerators;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Requests;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;
using Dotnet.OllamaSharp.LameChain.SDK.Interfaces.Command.Services;
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
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Text;

namespace DotnetLlamaSharp.Services.Prompting
{
    public class RagService : IRagService
    {
        private readonly IOllamaSharpService _chatService;
        private readonly IChromaService _chromaService;
        private readonly IPromptCommandsService _ollamaCommands;
        private readonly IEmbeddingsService _embeddingsService;
        private readonly IChromaChatsRepository _chatsRepo;
        private readonly ILogger<RagService> _logger;
        private readonly IMapper _mapper;
        private readonly ApiSettings _apiSettings;
        private readonly RequestOptions _settings;
        public RagService(IOllamaSharpService chatService, IChromaService chromaService, IChromaChatsRepository chatsRepo, IPromptCommandsService ollamaCommands,
            IEmbeddingsService embeddingsService, ILogger<RagService> logger, IMapper mapper, IOptions<ApiSettings> apiSettings, IOptions<RequestOptions> settings)
        {
            _chatService = chatService;
            _chromaService = chromaService;
            _logger = logger;
            _mapper = mapper;
            _chatsRepo = chatsRepo;
            _apiSettings = apiSettings.Value;
            _embeddingsService = embeddingsService;
            _ollamaCommands = ollamaCommands;
            _settings = settings.Value;
        }

        public async Task<RagPrompt> SimpleRagQuery(RagPromptRequest request, SmartRagSettings? smartSettings = null)
        {
            ChromaQuery chromaQuery = null;
            var collectionQueries = new Dictionary<string, ChromaQuery>();

            if (request.QueryCollections.Count > 0)
            {
                var queryPrompt = request.Prompt;

                if (smartSettings != null)
                {
                    if(smartSettings.WithRagExpansion)
                    {
                        var expansionReq = new RagExpansionRequest
                        {
                            Prompt = request.Prompt,
                            Results = smartSettings.RagExpansions,
                            Model = request.Settings.Model,
                            UsePrevAsExample = smartSettings.WithFewShotExpansion,
                            MaxExamples = smartSettings.MaxExamples,
                            Settings = _settings,
                        };

                        var expansion = await _ollamaCommands.PromptCommand<RagExpansionCommand, RagExpansionRequest, ChatMessage>(
                            expansionReq, instruction: null, request.Settings.GetType() == typeof(CommandSettings) ? (CommandSettings)request.Settings : null);


                        if (!string.IsNullOrEmpty(expansion.Content))
                            queryPrompt += $"\n{expansion.Content}";
                    }

                    if(smartSettings.WithQueryAugmentation)
                    {
                        var expansionReq = new RagExpansionRequest
                        {
                            Prompt = request.Prompt,
                            Results = smartSettings.QueryAugments,
                            Model = request.Settings.Model,
                            UsePrevAsExample = smartSettings.WithFewShotExpansion,
                            MaxExamples = smartSettings.MaxExamples,
                            Settings = _settings,
                        };

                        var expansion = await _ollamaCommands.PromptCommand<QueryAugmentationCommand, RagExpansionRequest, ChatMessage>(
                            expansionReq, instruction: null, request.Settings.GetType() == typeof(CommandSettings) ? (CommandSettings)request.Settings : null);

                        if (!string.IsNullOrEmpty(expansion.Content))
                            queryPrompt += $"\n{expansion.Content}";   
                    }
                }

                collectionQueries = await QueryCollections(request.QueryCollections, queryPrompt, request.CollectionRetrievals, request.MinDistance, request.EmbeddingFilters);
            }

            ///////////////////////////////////////// RagCommand //////////////////////////////////////
            /// aVeryBasicModel (simple secuencia en cadena. v1)
            ///     .And(QueryAumentationCommand)
            ///     .And(RagExpansionCommand)
            ///     .And(LangSearchCommand)
            /// ChatCommand.And(A)  ('enriquece' una peticion de chat)
            ///            .And(B)
            ///            .And(C)
            /// ------- GameBot -------
            /// ChatCommand.And(EvaluateSentimentCommand)
            ///            .And(GetChatChistoyCommand)
            ///            .And(GetLongTermMemosCommand)
            ///            .And(EvaluateCurrentMoodComand)
            ///            .And(EvaluateOutputActionCommand)
            ///            .And(ChromaChatDumpCommand)
            ///            
            /// ChatCommand(req).And(EvaluateSentiment
            ///                      .And(SelectMood))
            ///                 .And(ExtractNewLoreCommand
            ///                      .And(StoreLoreCommand)
            ///                 .And(ChromaChatDumpCommand)
            ///    
            /// [esto seria v2 / encapsular commands en metodos de extension muy concretos. sigue siendo todo secuencial ]
            /// aVeryBasicModelWithPromptAndGenerationSettings
            ///     .Augment(req.QueryAugments)
            ///     .Expand(req.RagExpansions)
            ///     .LangSearch(req.ResultsNumber)
            ///     .Rag(req) = Message 
            ///     .ChromaDump()  (store & return) Message
            ///     .ToPdf(path) | ToXFormat(path)
            ///     
            /// [SIEMPRE SE EMPIEZA POR CHAT | PROMPT COMMAND]
            /// aVeryBasicModel     inpt = Quien era el Maestro Canches?
            ///     .ExtractIntent() <- devuelve inpt + observacion / opt ("User wants to know who is Maestro Canches)
            ///     .Evaluate()     <- IsItRelatedToCollections ?
            ///     .Branch(out A = DoA() <- RagChain?
            ///               .MoreStuff()
            ///               .AndMoreStuff()
            ///         ,out var B = DoB() <- Chat | LangSearch
            ///                 .MoreBStuff()
            ///     isTrueContinue: true / false) <-- BRANCH siempre devuelve A | B y CONTINUA con A | B segun el bool, pero siempre es 'continue | fallback', 
            ///                                      y en fallback se pueden encadenar movidas para controlarlo
            ///                                      y asi no hace falta 'ContinueIf | ContinueWith' (ContWith puede ser un 'ReturnToTap')
            ///     .ContinueWith(A) <- esto depende del desarrollador, que sabe lo que quiere hacer si true / false y cual es cual (A, B)
            ///         .ContinueIf(A | B) <- Y SI SE DA LA OTRA OPCION? EN VEZ DE _NEXT.PROMPT DEVUELVO OTRA_OPCION.PROMPT()
            ///     .StoreResult() <- o pasa A o pasa B en ambos casos todo es MESSAGE -> opt-bridge = List<string>{input, opt1, opt2, opt3 ...} getFull() = es una instruccion acumulativa (SmartRag) | getOpt(idx)
            ///     .Return() <- esto ejecuta ya todo o es el 'GO'. no hace  falta porque todo devuelve MESSAGE
            ///     
            ///     .For([commands]) // AKA FAN_OUT
            ///     .DoXThatWillBeAppliedToEACHCommand()
            ///     .AndEverythingFromHereIsAppliedToEachCommandUntil()
            ///     .Collect(_nextCommand) <- ALL COMMANDS CHUNKED IN ONE SINGLE INSTRUCTION FOR _nextCommand // AKA FAN_IN
            ///     .AndNowIsASingleCommandPipeAgain()
            ///     
            ///     FAN_OUT / FAN_IN examples: mismo input procesado de N formas
            ///     .For({
            ///         CmdA.And(cmdB),
            ///         CmdC,
            ///         CmdA.And(cmdC),
            ///         CmdB.And(cmdX).And(cmdY).And(cmdZ)
            ///      })
            ///      .Collect(_summarizationCommand) <- las fuentes de sumarizacion se procesan diferente por las cadenas de comandos
            ///      .ChromaDump()
            ///      
            /// TODO ESTO ES V1 --> CHAINABLE COMMANDS (no CHAIN STEPS)
            /// 
            /// Una cosa es una commando y otra un step.
            /// El SDK permite:
            ///     Encadenar COMMANDS (con fluent api)
            ///     Secuenciar STEPS (sin fluent api. Un Step es una Dictionary<string, Command> que ejectua la cadena de CMDS secuencialmente y le pasa el output a otro Dict / STEP
            ///     Para que? -> Para poder hacer requests tipo Steps[
            ///     {
            ///        model &|| provider <- run step 1 w/Claude, step2 w/ollama etc
            ///        cmdNames[]
            ///     },
            ///     {
            ///         cmdNames
            ///     }
            ///     ]
            var baseIntruction = string.IsNullOrEmpty(request.SystemMessage) ? "" : request.SystemMessage.Trim();

            var systemChunk = await _chromaService.GetSysMessage("system-messages", "rag-query");
            var systemMessage = @$"
{(string.IsNullOrEmpty(baseIntruction) ? "You are a helpful assistant." : baseIntruction)}

{systemChunk.Text}

{getFileRagStringResult(collectionQueries)}";
        
            var chatRequest = _mapper.Map<RagPromptRequest, SimplePromptRequest>(request);

            chatRequest.SystemMessage = systemMessage;
      
            var response = await _chatService.SimplePrompt(chatRequest);

            if (!string.IsNullOrEmpty(response.Output))
                response.ChatHistory.Add(new ChatMessage(ChatRole.Assistant.ToString(), response.Output));

            string embeddingModels = string.Empty;
            collectionQueries.Keys.ToList().ForEach(name => embeddingModels += $"{collectionQueries[name].EmbeddingModel},");
            return new RagPrompt
            {
                Model = response.Model,
                EmbeddingModel = embeddingModels.Substring(0, embeddingModels.Length -1),
                Input = request.Prompt,
                Output = response.Output,
                IncludedChunks = collectionQueries.SelectMany(kvp => kvp.Value.Chunks).ToList(),
                ChatHistory = response.ChatHistory,
                InputEmbedding = collectionQueries.Count() > 0 ? collectionQueries.First().Value.QueryEmbedding : null
            };
        }

        public async Task<RagPrompt> SimpleSmartQuery(SmartQueryRequest request)
        {

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var userIntent = await _ollamaCommands.GuidedPromptCommand<PromptCommandRequest, ChatMessage>(
                new PromptCommandRequest(request.Prompt, request.Settings.ToOllamaRequest(), request.Settings.Model),
                guidanceMessage: null,
                request.Settings);

            var intentEvaluationPrompt = $"- USER INTENT = {userIntent.Content}";

            var collectionsCatalogue = await getFileCollectionsRagText();

            if (request.WithChatCollections)
                collectionsCatalogue += await getChatCollectionsRagText();

            var evaluationInstruction = $"Your task is to determine whether the provided User Intent is related to any of the Collections of the list below.\n\n>> AVAILABLE COLLECTIONS:\n\n{collectionsCatalogue}";

            var isRelatedIntent = await _ollamaCommands.ScoredBool(intentEvaluationPrompt, evaluationInstruction, request.Settings);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////


            var ragReq = _mapper.Map<SimpleCommandRequest, RagPromptRequest>(request);

            if (isRelatedIntent.Answer && isRelatedIntent.Score >= request.IntentConfidenceThreshold)
            {
                var choices = new Dictionary<string, string>();

                var availableCollections = await _chromaService.GetAllFileCollections();

                availableCollections.ForEach(collection => choices.Add(getFileCollectionRagText(collection), collection.Name));

                if(request.WithChatCollections)
                {
                    var availableChats = await _chromaService.GetAllChatCollections();

                    availableChats.ForEach(collection => choices.Add(getChatCollectionRagText(collection), collection.Name));
                }

                var selectorGuidance = "Select ONLY the 'COLLECTION NAME' value of the provided list OR empty list if there are no collections relevant for the user query.";

                var selectedChoices = await _ollamaCommands.MultiChoice(request.Prompt, choices.Keys.ToList(), maxChoices: request.MaxCollectionChoices, guidanceMessage: selectorGuidance, request.Settings);

                ragReq.SystemMessage = null;

                ragReq.CollectionRetrievals = Math.Max(0, request.CollectionRetrievals);

                selectedChoices.ForEach(selected => ragReq.QueryCollections.Add(selected.Split(" ")[0])); // in case the LLM fails to output only the collection name from the formatted catalogue values.  
            }

            ragReq.SystemMessage = request.SystemMessage;

            return await SimpleRagQuery(ragReq, _mapper.Map<SmartQueryRequest, SmartRagSettings>(request));
        }

        public async Task<ChatPrompt> RagChatPrompt(RagChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Prompt))
                throw new BadHttpRequestException("No prompt found. Type something to start chatting");

            if (string.IsNullOrEmpty(request.Settings.Model))
                request.Settings.Model = _apiSettings.DefaultModel;

            if (string.IsNullOrEmpty(request.AgentName))
                request.AgentName = request.Settings.Model;

            if (string.IsNullOrEmpty(request.AgentName.Trim()))
                throw new BadHttpRequestException($"No Agent Name has been set neither a llm model has been found to use in its place");

            if (string.IsNullOrEmpty(request.UserName.Trim()))
                request.UserName = _apiSettings.DefaultUserName;

            if (string.IsNullOrEmpty(request.UserName))
                throw new BadHttpRequestException("No user name has been found in the request and appsetings");

            if (string.IsNullOrEmpty(request.SystemMessage))
                request.SystemMessage = $"You are a helpful assistant named: {request.AgentName}. Your task is to chat with the user, {request.UserName}.";

            else request.SystemMessage += $"\n\nYour name is {request.AgentName}. You are starting a conversation with the user, {request.UserName}.";
            
            var collectionName = $"{request.AgentName.Trim().Replace(" ", "")}-{request.UserName.Trim().Replace(" ", "")}";
            
            var isFirstSession = !await _chatsRepo.CollectionExists(collectionName);

            ChromaChatsCollection collection = null;
            if (isFirstSession)    
                collection = await _chatsRepo.InitCollection(request.AgentName, request.UserName, _apiSettings.DefaultEmbedder, _apiSettings.DefaultDimensions);
            
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

                sessionChunk.UpdateType((int)EChunkType.SESSION);

                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES).ToLower(), 1);
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.CHAT_INIT).ToLower(), true);
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.CURRENT).ToLower(), true);
                sessionChunk.AddMetadata(nameof(ChatChunkMetadata.LLM_MODEL).ToLower(), request.Settings.Model, resetDefault: true);

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

                var sessionEmbeddings = await _chatsRepo.QueryCollection(collection.Name, currentEmbedding.GeneratedEmbeddings.FirstOrDefault().Vector, _apiSettings.RagChatMemoryRetrievals, memoFilters);

                chatMemos = _apiSettings.RagChatMemoMinDistance <= 0 ? sessionEmbeddings : sessionEmbeddings.Where(x => x.Distance <= _apiSettings.RagChatMemoMinDistance).ToList();

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
            
            if (currentChunk.Text.Length >= _apiSettings.RagChatChunkSize)
            {
                var overlaps = new List<ChatMessage> { new ChatMessage(ChatRole.User.ToString(), response.Input) };
                overlaps.Add(new ChatMessage(ChatRole.Assistant.ToString(), response.Output));
                var nextChunk = await generateNextChunk(currentChunk, sessionChunk, collection, overlaps);
            }

            return response;
        }

        public async Task<Dictionary<string, ChromaQuery>> QueryCollections(List<string> names, string text, int resultsNumber, double? minDistance, Dictionary<string, object> filters)
        {
            if (minDistance == 0)
                minDistance = null;

            var results = new Dictionary<string, ChromaQuery>();
            ChromaQuery queryResult = null;
            foreach (var name in names)
            {
               //TODO: Add chat sessions & handle CHAT_INIT false (returns 0 embeddings for chunks without the tag
                queryResult = await _chromaService.QueryCollection(name, text, resultsNumber, filters);

                if (minDistance != null)
                    queryResult.Chunks = queryResult.Chunks.Where(chunk => chunk.Distance <= minDistance).ToList();

                results.Add(name, queryResult);
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
            nextChunk.UpdateType((int)EChunkType.CHAT);
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

        private string getFileRagStringResult(Dictionary<string, ChromaQuery> collectionQueries)
        {
            var sb = new StringBuilder().Append("### RETRIEVED DATA:\n");

            collectionQueries.Keys.ToList()
                .ForEach(key => collectionQueries[key].Chunks
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

        public async Task<RagPrompt> ScoredBinaryQuestion(RagPromptRequest request)
        {
            return null;
            //var systemMessage = $"{request.SystemMessage.Trim()}";
            //ChromaQuery chromaQuery = null;
            //var collectionQueries = new Dictionary<string, List<ChromaQueryChunk>>();

            //if (request.QueryCollections.Count > 0)
            //{
            //    collectionQueries = await QueryCollections(request.QueryCollections, request.Prompt, request.CollectionRetrievals, request.MinDistance, request.EmbeddingFilters);
            //    systemMessage += $"{getFileRagStringResult(collectionQueries)}";
            //}

            //request.SystemMessage = systemMessage;

            //var response = await _chatService.BooleanQuestion(_mapper.Map<RagPromptRequest, SimplePromptRequest>(request));

            //return new RagPrompt
            //{
            //    Model = response.Model,
            //    EmbeddingModel = chromaQuery != null ? chromaQuery.EmbeddingModel : null,
            //    Input = request.Prompt,
            //    Output = response.Output,
            //    IncludedChunks = collectionQueries.SelectMany(kvp => kvp.Value).ToList(),
            //    ChatHistory = response.ChatHistory,
            //    InputEmbedding = chromaQuery != null ? chromaQuery.QueryEmbedding : null
            //};
        }


        private async Task<string> getFileCollectionsRagText(string itemSplitMark = null)
        {
            var sb = new StringBuilder();

            var collections = await _chromaService.GetAllFileCollections();

            collections.ForEach(collection => sb.Append(getFileCollectionRagText(collection, itemSplitMark)));

            return sb.ToString().Trim();
        }

        private async Task<string> getChatCollectionsRagText()
        {
            var sb = new StringBuilder();

            var collections = await _chromaService.GetAllChatCollections();

            collections.ForEach(collection => sb.Append(getChatCollectionRagText(collection)));

            return sb.ToString();
        }
        private string getFileCollectionRagText(ChromaFilesCollection collection, string itemSplitMark = null)
        {
            var sb = new StringBuilder();

            sb.Append(string.IsNullOrEmpty(itemSplitMark) ? "" : itemSplitMark)
                  .Append("\n\n- COLLECTION NAME =  ")
                  .Append(collection.Name)
                  .Append(string.IsNullOrEmpty(collection.Description) ? "" : $"\n- DESCRIPTION: {collection.Description}\n")
                  .Append("> TOPICS: ")
                  .Append(collection.GetMeta<FileCollectionMetadata>().TOPICS);

            return sb.ToString();
        }
        private string getChatCollectionRagText(ChromaChatsCollection collection)
        {
            var sb = new StringBuilder();

            sb.Append("\n\n- COLLECTION NAME =  ")
              .Append(collection.Name)
              .Append($" - {collection.Description}\n");

            return sb.ToString();
        }
    }
}
