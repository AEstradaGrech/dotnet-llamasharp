
using Dotnet.Chroma.Repositories.Models.Interfaces;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Core.Evaluators;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.AtomicValues;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Services;
using Dotnet.OllamaSharp.LameChain.SDK.Interfaces.Command.Services;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Domain.Services.Prompting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace DotnetLlamaSharp.Infrastructure.Services.LlmTools
{
    public class LlamaSharpTools : ToolsService<LlamaSharpTools>
    {
        private readonly ILogger<LlamaSharpTools> _logger;
        public LlamaSharpTools() : base() { }
        public LlamaSharpTools(IServiceProvider services, ILogger<LlamaSharpTools> logger) : base(services)
        {
            _logger = logger;
        }

        [Description("Tool to select the best ChromaDB collection to query based on the user input.")]
        public async Task<string> ChromaCollectionSelector(
            [Description("User input to analyze to extract the intent and select the best collection")] string userQuery)
        {
            _logger.LogWarning($"USING TOOL: {nameof(ChromaCollectionSelector)}");

            var commandsFactory = _services.GetRequiredService<IPromptCommandsFactory>();
            var ragService = _services.GetRequiredService<IRagService>();

            var intentCommand = commandsFactory.GetCommand<UserIntentCommand, ChatMessage>();

            var request = new PromptCommandRequest(userQuery);

            var intent = await intentCommand.Prompt(request); 

            _logger.LogWarning($"TOOL_CALL >> {nameof(ChromaCollectionSelector)} >> USER INTENT: {intent.Content}");

            var selectorGuidance = $"# IMPORTANT: This is the analysis of the user intent, use it to be more accurate in your selection: {intent.Content}";

            var collectionsCat = await ragService.GetChromaCollectionChoices(withChatCollections: false);

            var choiceCommand = commandsFactory.GetStringChoiceCommand();

            selectorGuidance += "\n# IMPORTANT: Select ONLY the 'COLLECTION NAME' value of the provided list OR empty list if there are no collections relevant for the user query.";

            var selectorPrompt = $"Select the best collection to retrieve data from given this user query: {userQuery}";

            _logger.LogWarning($"TOOL_CALL >> {nameof(ChromaCollectionSelector)} >> SELECTOR PROMPTS:\n>> USER PROMPT: {selectorPrompt}\n>> GUIDANCE: {selectorGuidance}");

            var choiceReq = new StringChoiceRequest(collectionsCat, selectorPrompt, guidance: selectorGuidance, isGuidanceAppend: false, model: null);

            var choice = await choiceCommand.Prompt(choiceReq);

            _logger.LogWarning($"TOOL_CALL >> {nameof(ChromaCollectionSelector)} >> SELECTED: {choice}");

            return choice;
        }

        //[Description($"Tool to retrieve data from the specified collection Chroma and user query. This tool is complementary to the {nameof(ChromaCollectionSelector)} tool and MUST be used AFTER calling the {nameof(ChromaCollectionSelector)} tool")]
        // Note: the above description includes instructions about how to orchestrate the tools and it is necessary to use tools without a skill or system instruction specifying how to use the tools. It is here for demonstrative purposes (use LameAgents + Skills)

        [Description($"Tool to retrieve data from the specified Chroma collectionand input query.")]
        public async Task<List<string>> ChromaSearchTool(
            [Description("Name of the ChromaDB collection to query")] string collectionName,
            [Description("Text input that will be used to query the specified chroma collection")] string userQuery)
        {
            _logger.LogWarning($"USING TOOL: {nameof(ChromaSearchTool)}");

            var chromaService = _services.GetRequiredService<IChromaService>();

            _logger.LogWarning($"TOOL_CALL: {nameof(ChromaSearchTool)} >> retrieving collection: {collectionName}");

            var collection = await chromaService.GetCollection(collectionName.Replace("\"", ""));

            if (collection == null) return [];

            _logger.LogWarning($"USING TOOL: {nameof(ChromaSearchTool)} >> retrieved collection {collectionName} >> Embedding Model: {collection.DefaultMetadata.MODEL} >> Embedding Dimensions: {collection.DefaultMetadata.DIMENSIONS}");

            var commandsFactory = _services.GetRequiredService<IPromptCommandsFactory>();

            var searchCommand = commandsFactory.GetVectorSearchSourceable(chromaService.SimilaritySearch);

            var request = new VectorSearchRequest(collectionName, userQuery, collection.DefaultMetadata.MODEL, collection.DefaultMetadata.DIMENSIONS, 4);

            _logger.LogWarning($"TOOL_CALL: {nameof(ChromaSearchTool)} >> User query: {userQuery}");

            return await searchCommand.Prompt(request);
        }

        [Description($"Tool to save text data to the specified Chroma collection.")]
        public async Task ChromaTextSave(
            [Description("Name of the ChromaDB collection to save the text to")] string collectionName,
            [Description("Text input that will be saved to the specified chroma collection")] string text)
        {
            _logger.LogWarning($"USING TOOL: {nameof(ChromaTextSave)}");

            var chromaService = _services.GetRequiredService<IChromaService>();

            _logger.LogWarning($"TOOL_CALL: {nameof(ChromaTextSave)} >> saving to collection: {collectionName}");

            var insert = await chromaService.StoreChunk(collectionName.Replace("\"", ""), text);

            if(insert != null)
                _logger.LogWarning($"TOOL_CALL: {nameof(ChromaTextSave)} >> Text: {text}");

            else _logger.LogError($"TOOL_CALL: {nameof(ChromaTextSave)} >> failed to save text: {text}");
        }

        [Description($"Tool to retrieve a catalogue of all the available Chroma collections along with their details.")]
        public async Task<string> ChromaDatabaseExplorer()
        {
            _logger.LogWarning($"USING TOOL: {nameof(ChromaDatabaseExplorer)}");

            var defaultRepo = _services.GetRequiredService<IChromaChunksRepository>();

            return await defaultRepo.GetCollectionsInfo();
        }
    }
}
