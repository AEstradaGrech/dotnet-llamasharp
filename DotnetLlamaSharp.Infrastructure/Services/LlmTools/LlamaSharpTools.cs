
using Dotnet.OllamaSharp.LameChain.SDK.Command.Core.Evaluators;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.AtomicValues;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Interfaces;
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

        [Description("Tool to select the best ChromaDB collection to query based on the user input. Use this tool to get the name of the collection that best matches with the user intent.")]
        public async Task<string> ChromaCollectionSelector(
            [Description("User input to analyze to extract the intent and select the best collection")] string userQuery)
        {
            _logger.LogWarning($"USING TOOL: {nameof(ChromaCollectionSelector)}");

            //using var scope = _services.CreateScope(); 

            var commandsFactory = _services.GetRequiredService<IPromptCommandsFactory>();
            var ragService = _services.GetRequiredService<IRagService>();

            var intentCommand = commandsFactory.GetCommand<UserIntentCommand, ChatMessage>();

            var request = new PromptCommandRequest(userQuery);

            var intent = await intentCommand.Prompt(request); // So far so good, but then it crashes here with the second LLM request

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

        public List<string> ChromaSearchTool(
            [Description("Name of the ChromaDB collection to query")] string collectionName,
            [Description("User input that will be used to query the specified chroma collection")] string userQuery)
        {
            //de donde saco los repos y los comandos
            return new List<string>();
        }
    }
}
