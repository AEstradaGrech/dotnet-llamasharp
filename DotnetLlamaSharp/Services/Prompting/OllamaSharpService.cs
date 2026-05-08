using AutoMapper;
using DocumentFormat.OpenXml.EMMA;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using DotnetLlamaSharp.Domain.Services.Prompting;
using Microsoft.Extensions.Options;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using MicrosoftAI = Microsoft.Extensions.AI;

namespace DotnetLlamaSharp.Services.Prompting
{
    // FYI: https://elbruno.com/tag/ollama/
    public class OllamaSharpService : IOllamaSharpService
    {
        private readonly IOllamaInferenceService _ollamaService;
        private readonly IPromptCommandsFactory _promptsFactory;

        private readonly RequestOptions _settings;
        private readonly ILogger<OllamaSharpService> _logger;

        public OllamaSharpService(IOllamaInferenceService ollamaService, IPromptCommandsFactory promptsFactory, IOptions<RequestOptions> settings, ILogger<OllamaSharpService> logger)
        {
            _ollamaService = ollamaService ?? throw new ArgumentNullException($"{nameof(IOllamaInferenceService)}");
            _promptsFactory = promptsFactory;
            _settings = settings.Value ?? new RequestOptions();
            _logger = logger;

            if (settings.Value == null)
                _logger.LogWarning("-- No RequestOptions injected from app settings. Using default OllamaSharp request options --");
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

            var command = _promptsFactory.GetMessagePromptCommand(request.SystemMessage);

            var response = await command.Prompt(_ollamaService, new PromptCommandRequest { Model = request.Model, Prompt = request.Prompt, Settings = _settings });

            return new ChatPrompt { Model = request.Model, Input = request.Prompt, Output = response.Content ?? "", ChatHistory = messages };
        }

        // call to '/api/chat' endpoint to handle user-assistant chat turns
        public async Task<ChatPrompt> ChatPrompt(ChatPromptRequest request, bool bWithSysmsgUpdate)
        {
            var sb = new System.Text.StringBuilder();

            var command = _promptsFactory.GetMessagePromptCommand(request.SystemMessage);

            var response = await command.Prompt(_ollamaService, new ChatCommandRequest { Model = request.Model, Prompt = request.Prompt, Settings = _settings, ChatHistory = request.ChatHistory, IncludeSystemMessage = bWithSysmsgUpdate });

            request.ChatHistory.Add(new ChatMessage(response.Role.ToString(), response.Content));

            return new ChatPrompt { Model = request.Model, Input = request.Prompt, Output = response.Content ?? "", ChatHistory = request.ChatHistory };
        }

        public async Task<EmbedResponse> GetOllamaClientEmbeddings(EmbedRequest request)
        {
            request.Options = _settings;

            return await _ollamaService.GetEmbeddings(request);
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
