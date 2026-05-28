using Dotnet.Chroma.Repositories.Models;
using Dotnet.LangSearch.SDK;
using Dotnet.LangSearch.SDK.Models.Request;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Core.Evaluators;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Core.TextGenerators;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Responses.StructuredOutputs;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Core.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.AtomicValues;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Extensions;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Interfaces.Command.Services;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Request;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Response;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Step;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Step.ValueObjects;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Domain.Services.Prompting;
using Microsoft.Extensions.Options;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Services.Prompting
{
    // FYI: https://elbruno.com/tag/ollama/
    public class OllamaSharpService : IOllamaSharpService
    {
        //private readonly IOllamaInferenceService _ollamaService;
        private readonly IPromptCommandsFactory _promptsFactory;
        private readonly ILangSearchService _langSearch;
        private readonly IChromaService _chromaService;
        private readonly IChromaSysChunksRepository _sysRepo;
        private readonly RequestOptions _settings;
        private readonly ILogger<OllamaSharpService> _logger;

        public OllamaSharpService(IOptions<RequestOptions> settings, ILogger<OllamaSharpService> logger)
        {
            _promptsFactory = promptsFactory;
            _langSearch = langSearch;
            _settings = settings.Value ?? new RequestOptions();
            _logger = logger;
            _chromaService = chromaService;

            if (settings.Value == null)
                _logger.LogWarning("-- No RequestOptions injected from app settings. Using default OllamaSharp request options --");
            _sysRepo = sysRepo;
        }

        // call to '/api/generate' endpoint for single Q&A inference
        public async Task<ChatPrompt> SimplePrompt(SimplePromptRequest request)
        {
            if (string.IsNullOrEmpty(request.SystemMessage))
                request.SystemMessage = "You are a helpful assistant";

            var messages = new List<ChatMessage> {
                new ChatMessage(ChatRole.System.ToString(), request.SystemMessage),
                new ChatMessage(ChatRole.User.ToString(), request.Prompt)
            };

            var command = _promptsFactory.GetMessagePromptCommand(request.SystemMessage, request.Settings);

            var response = await command.Prompt(new PromptCommandRequest { Model = request.Settings.Model, Prompt = request.Prompt });

            return new ChatPrompt { Model = request.Settings.Model, Input = request.Prompt, Output = response.Content ?? "", ChatHistory = messages };
        }

        // call to '/api/chat' endpoint to handle user-assistant chat turns
        public async Task<ChatPrompt> ChatPrompt(ChatPromptRequest request, bool bWithSysmsgUpdate)
        {
            var sb = new System.Text.StringBuilder();

            var command = _promptsFactory.GetMessagePromptCommand(request.SystemMessage, request.Settings);

            var response = await command.Prompt(new ChatCommandRequest { Model = request.Settings.Model, Prompt = request.Prompt, ChatHistory = request.ChatHistory, IncludeSystemMessage = bWithSysmsgUpdate });

            request.ChatHistory.Add(new ChatMessage(response.Role.ToString(), response.Content));

            return new ChatPrompt { Model = request.Settings.Model, Input = request.Prompt, Output = response.Content ?? "", ChatHistory = request.ChatHistory };
        }

        public async Task<EmbedResponse> GetOllamaClientEmbeddings(EmbedRequest request)
        {
            request.Options = _settings;

            return null; //TODO: EMBEDDINGS COMMAND await _ollamaService.GetEmbeddings(request);
        }

        //public async Task<ChatPrompt> BooleanQuestion(SimplePromptRequest req)
        //{
        //    var command = _promptsFactory.GetBoolPromptCommand("bool-resp");

        //    var response = await command.Prompt(_ollamaService, new PromptCommandRequest { Model = req.Model, Prompt = req.Prompt, Settings = _settings });

        //    if (response == null)
        //        throw new ArgumentNullException($"{nameof(OllamaSharpService)} >> {nameof(BooleanQuestion)} >> An error has ocurred while requesting the structured output, try again");

        //    var stringResponse = response ? "YES" : "NO";

        //    var messages = new List<ChatMessage> { new ChatMessage(ChatRole.System.ToString(), req.SystemMessage), new ChatMessage(ChatRole.User.ToString(), req.Prompt)};

        //    messages.Add(new ChatMessage(ChatRole.Assistant.ToString(), stringResponse));

        //    return new ChatPrompt { Model = req.Model, Input = req.Prompt, Output =  stringResponse, ChatHistory = messages };
        //}

    }
}
