using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Interfaces.Command.Services;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Services.Prompting;
using Microsoft.Extensions.Options;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Services.Prompting
{
    // FYI: https://elbruno.com/tag/ollama/
    public class OllamaSharpService : IOllamaSharpService
    {
        private readonly IPromptCommandsFactory _promptsFactory;
        private readonly RequestOptions _settings;
        private readonly ILogger<OllamaSharpService> _logger;

        public OllamaSharpService(IPromptCommandsFactory factory, IOptions<RequestOptions> settings, ILogger<OllamaSharpService> logger)
        {
            _promptsFactory = factory ?? throw new ArgumentNullException($"{nameof(IPromptCommandsFactory)}");
            _settings = settings.Value ?? new RequestOptions();
            _logger = logger;
           
            if (settings.Value == null)
                _logger.LogWarning("-- No RequestOptions injected from app settings. Using default OllamaSharp request options --");
        }

        // call to '/api/generate' endpoint for single Q&A inference
        public async Task<ChatPrompt> SimplePrompt(SimpleCommandRequest request)
        {
            if (string.IsNullOrEmpty(request.SystemMessage))
                request.SystemMessage = "You are a helpful assistant";

            var messages = new List<ChatMessage> {
                new ChatMessage(ChatRole.System.ToString(), request.SystemMessage),
                new ChatMessage(ChatRole.User.ToString(), request.Prompt)
            };

            var command = _promptsFactory.GetMessagePromptCommand(request.SystemMessage, request.Settings);

            var response = await command.Prompt(new PromptCommandRequest { Model = request.Settings.Model, Prompt = request.Prompt });

            return new ChatPrompt { Model = request.Settings.Model, Input = request.Prompt, Output = response.Content ?? "", ChatHistory = messages, Timestamp = DateTime.Now };
        }

        // call to '/api/chat' endpoint to handle user-assistant chat turns
        public async Task<ChatPrompt> ChatPrompt(CommandChatRequest request, bool bWithSysmsgUpdate)
        {
            var sb = new System.Text.StringBuilder();

            var command = _promptsFactory.GetMessagePromptCommand(request.SystemMessage, request.Settings);

            var response = await command.Prompt(new ChatCommandRequest { Model = request.Settings.Model, Prompt = request.Prompt, ChatHistory = request.ChatHistory, IncludeSystemMessage = bWithSysmsgUpdate });

            request.ChatHistory.Add(new ChatMessage(response.Role.ToString(), response.Content));

            return new ChatPrompt { Model = request.Settings.Model, Input = request.Prompt, Output = response.Content ?? "", ChatHistory = request.ChatHistory, Timestamp = DateTime.Now };
        }

        public async Task<EmbedResponse> GetOllamaClientEmbeddings(EmbedRequest request)
        {
            request.Options = _settings;

            return null; //TODO: EMBEDDINGS COMMAND await _ollamaService.GetEmbeddings(request);
        }

    }
}
