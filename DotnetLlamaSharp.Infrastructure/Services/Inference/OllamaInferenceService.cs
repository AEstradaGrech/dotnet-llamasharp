using DocumentFormat.OpenXml.Wordprocessing;
using DotnetLlamaSharp.Domain.Services.Inference;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Net.Security;
using System.Text.Json;
using System.Text.Json.Schema;


namespace DotnetLlamaSharp.Infrastructure.Services.Inference
{
    public class OllamaInferenceService : IOllamaInferenceService
    {
        private readonly IOllamaApiClient _client;
        private readonly ApiSettings _apiSettings;
        private readonly RequestOptions _settings;
        public OllamaInferenceService(IOllamaApiClient client, IOptions<ApiSettings> apiSettings, IOptions<RequestOptions> settings)
        {
            _client = client;
            _apiSettings = apiSettings.Value;
            _settings = settings.Value;
        }
       
        public async Task<Message> SimplePrompt(GenerateRequest request)
        {
            var sb = new System.Text.StringBuilder();

            request.Stream = false;

            await foreach (var part in _client.GenerateAsync(request))
                if (!string.IsNullOrEmpty(part?.Response))
                    sb.Append(part.Response);

            return new Message(ChatRole.Assistant.ToString(), sb.ToString());
        }

        public async Task<Message> ChatPrompt(ChatRequest request)
        {
            var sb = new System.Text.StringBuilder();

            await foreach (var part in _client.ChatAsync(request))
                if (!string.IsNullOrEmpty(part?.Message.Content))
                    sb.Append(part.Message.Content);

            return new Message(ChatRole.Assistant.ToString(), sb.ToString());
        }

        public IAsyncEnumerable<GenerateResponseStream?> SimplePromptStream(GenerateRequest request)
        {
            request.Stream = true;

            return _client.GenerateAsync(request);
        }

        public IAsyncEnumerable<ChatResponseStream?> ChatPromptStream(ChatRequest request)
        {
            request.Stream = true;

            return _client.ChatAsync(request);
        }

        public async Task<EmbedResponse> GetEmbeddings(EmbedRequest request)
            => await _client.EmbedAsync(request);

        public async Task<T> StructuredPrompt<T>(string finalMessage, string? systemGuidance = null, string? jsonModel = null) where T : class
        {
            if (string.IsNullOrEmpty(finalMessage) && string.IsNullOrEmpty(systemGuidance))
                throw new InvalidDataException($"{nameof(OllamaInferenceService)} >> {nameof(StructuredPrompt)} >> no messages to send");

            var request = new ChatRequest
            {
                Model = string.IsNullOrEmpty(jsonModel) ? _apiSettings.JsonModels[0] : _apiSettings.JsonModels.Contains(jsonModel) ? jsonModel : _apiSettings.JsonModels[0],
                Messages = !string.IsNullOrEmpty(systemGuidance) ? [new Message { Role = ChatRole.System, Content = systemGuidance }, new Message { Role = ChatRole.User, Content = finalMessage}] : [new Message { Role = ChatRole.User, Content = finalMessage }],
                Format = JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(T)),
                Stream = false
            };
            
            var sb = new System.Text.StringBuilder();
          
            await foreach (var part in _client.ChatAsync(request))
                if (!string.IsNullOrEmpty(part?.Message.Content))
                    sb.Append(part.Message.Content);

            return JsonSerializer.Deserialize<T>(sb.ToString());
        }
    }
}
