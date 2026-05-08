using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Services.Inference;
using DotnetLlamaSharp.Domain.Services.Prompting;
using Microsoft.Extensions.Options;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Services.Prompting
{
    public class OllamaStreamService : IOllamaStreamService
    {
        private readonly IOllamaInferenceService _ollamaService;
        private readonly RequestOptions _settings;
        public OllamaStreamService(IOllamaInferenceService ollamaService, IOptions<RequestOptions> settings)
        {
            _ollamaService = ollamaService;
            _settings = settings.Value;
        }
        public IAsyncEnumerable<GenerateResponseStream?> SimplePromptStream(SimplePromptRequest request)
           => _ollamaService.SimplePromptStream(getGenerateRequest(request, isStream: true));

        public IAsyncEnumerable<ChatResponseStream?> ChatPromptStream(ChatPromptRequest request, bool bWithSysmsgUpdate = false)
        {
            setRequestForChat(request, bWithSysmsgUpdate);

            return _ollamaService.ChatPromptStream(getChatRequest(request, isStream: true));
        }

        private GenerateRequest getGenerateRequest(SimplePromptRequest promptRequest, bool isStream = false)
        {
            var settings = _settings;
            settings.NumPredict = promptRequest.MaxTokens;
            settings.Temperature = promptRequest.Temperature;
            return new GenerateRequest
            {
                Model = promptRequest.Model,
                Prompt = promptRequest.Prompt,
                System = promptRequest.SystemMessage,
                Stream = isStream,
                Options = settings
            };
        }

        private ChatRequest getChatRequest(ChatPromptRequest promptRequest, bool isStream = false)
        {
            var settings = _settings;
            settings.NumPredict = promptRequest.MaxTokens;
            settings.Temperature = promptRequest.Temperature;

            var messages = new List<Message>();

            promptRequest.ChatHistory.ForEach(x => messages.Add(new Message(new ChatRole(x.Role), x.Content)));

            return new ChatRequest
            {
                Model = promptRequest.Model,
                Messages = messages,
                Stream = isStream,
                Options = settings
            };
        }

        private ChatPromptRequest setRequestForChat(ChatPromptRequest request, bool bWithSysmsgUpdate)
        {
            if (request.ChatHistory.Count == 0)
            {
                if (string.IsNullOrEmpty(request.SystemMessage))
                    request.SystemMessage = "You are a helpful assistant";

                request.ChatHistory.Add(new ChatMessage(ChatRole.System.ToString(), request.SystemMessage));
            }

            if (bWithSysmsgUpdate && !string.IsNullOrEmpty(request.SystemMessage))
                request.ChatHistory.Add(new ChatMessage(ChatRole.System.ToString(), request.SystemMessage));

            request.ChatHistory.Add(new ChatMessage(ChatRole.User.ToString(), request.Prompt));

            return request;
        }
    }
}
