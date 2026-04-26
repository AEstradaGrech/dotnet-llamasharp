using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;


namespace DotnetLlamaSharp.Infrastructure.Services.Inference
{
    public class OllamaInferenceService : IOllamaInferenceService
    {
        private readonly IOllamaApiClient _client;

        public OllamaInferenceService(IOllamaApiClient client)
        {
            _client = client;
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
    }
}
