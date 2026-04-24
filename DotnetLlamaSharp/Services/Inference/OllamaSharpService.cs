using DotnetLlamaSharp.Models.Api.Prompting;
using DotnetLlamaSharp.Models.Api.Prompting.Request;
using DotnetLlamaSharp.Models.Api.Prompting.Response;
using DotnetLlamaSharp.Services.Inference.Interfaces;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Services.Inference
{
    // FYI: https://elbruno.com/tag/ollama/
    public class OllamaSharpService : IOllamaSharpService
    {
        private readonly IOllamaApiClient _client;
        private readonly RequestOptions _settings;
        private readonly ILogger<OllamaSharpService> _logger;
        public OllamaSharpService(IOllamaApiClient client, IOptions<RequestOptions> settings, ILogger<OllamaSharpService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(IOllamaApiClient));
            _settings = settings.Value ?? new RequestOptions();
            _logger = logger;
            if (settings.Value == null)
                _logger.LogWarning("-- No RequestOptions injected from app settings. Using default OllamaSharp request options --");
        }

        // call to '/api/generate' endpoint for single Q&A inference
        public async Task<ChatPromptResponse> SimplePrompt(ChatPromptRequest request)
        {
            if (string.IsNullOrEmpty(request.SystemMessage))
                request.SystemMessage = "You are a helpful assistant";

            var chatResponse = new List<ChatMessageDto> { 
                new ChatMessageDto(ChatRole.System.ToString(), request.SystemMessage),
                new ChatMessageDto(ChatRole.User.ToString(), request.Prompt)
            };
            
            var sb = new System.Text.StringBuilder();
            
            await foreach (var part in _client.GenerateAsync(GetGenerateRequest(request, isStream: false)))
                if (!string.IsNullOrEmpty(part?.Response))
                    sb.Append(part.Response);
            
            chatResponse.Add(new ChatMessageDto(ChatRole.Assistant.ToString(), sb.ToString()));

            return new ChatPromptResponse { ChatHistory = chatResponse };
        }

        public IAsyncEnumerable<GenerateResponseStream?> SimplePromptStream(ChatPromptRequest request)
            => _client.GenerateAsync(GetGenerateRequest(request, isStream: true));
        

        // call to '/api/chat' endpoint to handle user-assistant chat turns
        public async Task<ChatPromptResponse> ChatPrompt(ChatPromptRequest request, bool bWithSysmsgUpdate)
        {
            var sb = new System.Text.StringBuilder();

            SetRequestForChat(request, bWithSysmsgUpdate);

            await foreach (var part in _client.ChatAsync(GetChatRequest(request)))
                if (!string.IsNullOrEmpty(part?.Message.Content))
                    sb.Append(part.Message.Content);

            request.ChatHistory.Add(new ChatMessageDto(ChatRole.Assistant.ToString(), sb.ToString()));

            return new ChatPromptResponse { ChatHistory = request.ChatHistory };
        }

        public IAsyncEnumerable<ChatResponseStream?> ChatPromptStream(ChatPromptRequest request, bool bWithSysmsgUpdate = false)
        {
            SetRequestForChat(request, bWithSysmsgUpdate);

            return _client.ChatAsync(GetChatRequest(request, isStream: true));
        }

        private GenerateRequest GetGenerateRequest(ChatPromptRequest promptRequest, bool isStream = false)
        {
            var settings = _settings;
            settings.NumPredict = promptRequest.MaxTokens;
            settings.Temperature = promptRequest.Temperature;
            return new GenerateRequest {
                Model = promptRequest.Model,
                Prompt = promptRequest.Prompt,
                System = promptRequest.SystemMessage,
                Stream = isStream,
                Options = settings
            };
        }

        private ChatRequest GetChatRequest(ChatPromptRequest promptRequest, bool isStream = false)
        {
            var settings = _settings;
            settings.NumPredict = promptRequest.MaxTokens;
            settings.Temperature = promptRequest.Temperature;
            
            var messages = new List<Message>();

            promptRequest.ChatHistory.ForEach(x => messages.Add(new Message(new ChatRole(x.Role), x.Message)));
            
            return new ChatRequest {
                Model = promptRequest.Model,
                Messages = messages,
                Stream = isStream,
                Options = settings
            };
        }

        private ChatPromptRequest SetRequestForChat(ChatPromptRequest request, bool bWithSysmsgUpdate)
        {
            if (request.ChatHistory.Count == 0)
            {
                if (string.IsNullOrEmpty(request.SystemMessage))
                    request.SystemMessage = "You are a helpful assistant";
                request.ChatHistory.Add(new ChatMessageDto(ChatRole.System.ToString(), request.SystemMessage));
            }

            if (bWithSysmsgUpdate && !string.IsNullOrEmpty(request.SystemMessage))
                request.ChatHistory.Add(new ChatMessageDto(ChatRole.System.ToString(), request.SystemMessage));

            request.ChatHistory.Add(new ChatMessageDto(ChatRole.User.ToString(), request.Prompt));

            return request;
        }
    }
}
