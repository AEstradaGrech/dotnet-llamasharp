using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Services.Inference;
using DotnetLlamaSharp.Domain.Services.Prompting;
using DotnetLlamaSharp.Models.Common;
using DotnetLlamaSharp.Models.Request;
using DotnetLlamaSharp.Models.Response;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using MicrosoftAI = Microsoft.Extensions.AI;

namespace DotnetLlamaSharp.Services.Prompting
{
    // FYI: https://elbruno.com/tag/ollama/
    public class OllamaSharpService : IOllamaSharpService
    {
        private readonly IOllamaInferenceService _ollamaService;
        private readonly IMapper _mapper;
        private readonly MicrosoftAI.IEmbeddingGenerator<string, MicrosoftAI.Embedding<float>> _embeddingsGenerator;
        private readonly RequestOptions _settings;
        private readonly ILogger<OllamaSharpService> _logger;
        public OllamaSharpService(IOllamaInferenceService ollamaService, MicrosoftAI.IEmbeddingGenerator<string, MicrosoftAI.Embedding<float>> embeddingsGenerator, 
            IOptions<RequestOptions> settings, IMapper mapper, ILogger<OllamaSharpService> logger)
        {
            _ollamaService = ollamaService ?? throw new ArgumentNullException($"{nameof(IOllamaInferenceService)}");
            _embeddingsGenerator = embeddingsGenerator;
            _settings = settings.Value ?? new RequestOptions();
            _mapper = mapper;
            _logger = logger;
            if (settings.Value == null)
                _logger.LogWarning("-- No RequestOptions injected from app settings. Using default OllamaSharp request options --");
        }

        public async Task<MicrosoftAI.GeneratedEmbeddings<MicrosoftAI.Embedding<float>>> GenerateEmbeddings(string text)
            => await _embeddingsGenerator.GenerateAsync(new List<string> { text });
        public async Task<MicrosoftAI.GeneratedEmbeddings<MicrosoftAI.Embedding<float>>> GenerateEmbeddings(List<string> texts)
            => await _embeddingsGenerator.GenerateAsync(texts);

        // call to '/api/generate' endpoint for single Q&A inference
        public async Task<ChatPrompt> SimplePrompt(ChatPromptRequest request)
        {
            if (string.IsNullOrEmpty(request.SystemMessage))
                request.SystemMessage = "You are a helpful assistant";

            request.ChatHistory = new List<ChatMessage> {
                new ChatMessage(ChatRole.System.ToString(), request.SystemMessage),
                new ChatMessage(ChatRole.User.ToString(), request.Prompt)
            };

            var response = await _ollamaService.SimplePrompt(getGenerateRequest(request, isStream: false));

            request.ChatHistory.Add(_mapper.Map<Message, ChatMessage>(response));

            return new ChatPrompt { Model = request.Model, Input = request.Prompt, Output = response.Content ?? "", ChatHistory = request.ChatHistory };
        }

        public IAsyncEnumerable<GenerateResponseStream?> SimplePromptStream(ChatPromptRequest request)
            => _ollamaService.SimplePromptStream(getGenerateRequest(request, isStream: true));


        // call to '/api/chat' endpoint to handle user-assistant chat turns
        public async Task<ChatPrompt> ChatPrompt(ChatPromptRequest request, bool bWithSysmsgUpdate)
        {
            var sb = new System.Text.StringBuilder();

            setRequestForChat(request, bWithSysmsgUpdate);
            
            var response = await _ollamaService.ChatPrompt(getChatRequest(request, isStream: false));

            request.ChatHistory.Add(_mapper.Map<Message, ChatMessage>(response));

            return new ChatPrompt { Model = request.Model, Input = request.Prompt, Output = response.Content ?? "", ChatHistory = request.ChatHistory };
        }

        public IAsyncEnumerable<ChatResponseStream?> ChatPromptStream(ChatPromptRequest request, bool bWithSysmsgUpdate = false)
        {
            setRequestForChat(request, bWithSysmsgUpdate);

            return _ollamaService.ChatPromptStream(getChatRequest(request, isStream: true));
        }

        public async Task<EmbedResponse> GetOllamaClientEmbeddings(EmbedRequest request)
        {
            request.Options = _settings;

            return await _ollamaService.GetEmbeddings(request);
        }
        private GenerateRequest getGenerateRequest(ChatPromptRequest promptRequest, bool isStream = false)
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
