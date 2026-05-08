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
        private readonly IMapper _mapper;
        private readonly MicrosoftAI.IEmbeddingGenerator<string, MicrosoftAI.Embedding<float>> _embeddingsGenerator;
        private readonly RequestOptions _settings;
        private readonly ILogger<OllamaSharpService> _logger;
        private readonly IChromaSysChunksRepository _sysRepo;
        public OllamaSharpService(IOllamaInferenceService ollamaService, MicrosoftAI.IEmbeddingGenerator<string, MicrosoftAI.Embedding<float>> embeddingsGenerator, 
               IPromptCommandsFactory promptsFactory, IChromaSysChunksRepository sysRepo, IOptions<RequestOptions> settings, IMapper mapper, ILogger<OllamaSharpService> logger)
        {
            _ollamaService = ollamaService ?? throw new ArgumentNullException($"{nameof(IOllamaInferenceService)}");
            _promptsFactory = promptsFactory;
            _sysRepo = sysRepo;
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

        public IAsyncEnumerable<GenerateResponseStream?> SimplePromptStream(SimplePromptRequest request)
            => _ollamaService.SimplePromptStream(getGenerateRequest(request, isStream: true));


        // call to '/api/chat' endpoint to handle user-assistant chat turns
        public async Task<ChatPrompt> ChatPrompt(ChatPromptRequest request, bool bWithSysmsgUpdate)
        {
            var sb = new System.Text.StringBuilder();

            setRequestForChat(request, bWithSysmsgUpdate);

            var command = _promptsFactory.GetMessagePromptCommand(request.SystemMessage);

            var response = await command.Prompt(_ollamaService, new ChatCommandRequest { Model = request.Model, Prompt = request.Prompt, Settings = _settings, ChatHistory = request.ChatHistory });

            request.ChatHistory.Add(new ChatMessage(response.Role.ToString(), response.Content));

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

        public async Task<ChatPrompt> BooleanQuestion(SimplePromptRequest req)
        {
            var command = _promptsFactory.GetBoolPromptCommand("bool-resp");

            var response = await command.Prompt(_ollamaService, new PromptCommandRequest { Model = req.Model, Prompt = req.Prompt, Settings = _settings });
            
            if (response == null)
                throw new ArgumentNullException($"{nameof(OllamaSharpService)} >> {nameof(BooleanQuestion)} >> An error has ocurred while requesting the structured output, try again");
            
            var stringResponse = response ? "YES" : "NO";

            var messages = new List<ChatMessage> { new ChatMessage(ChatRole.System.ToString(), req.SystemMessage), new ChatMessage(ChatRole.User.ToString(), req.Prompt)};

            messages.Add(new ChatMessage(ChatRole.Assistant.ToString(), stringResponse));

            return new ChatPrompt { Model = req.Model, Input = req.Prompt, Output =  stringResponse, ChatHistory = messages };
        }

        public async Task<TEnum?> EnumChoice<TEnum>(string prompt, string? instruction = null) where TEnum : struct, Enum
        {
            var command = _promptsFactory.GetEnumChoiceCommand<TEnum>("enum-choice-resp", instruction);

            return await command.Prompt(_ollamaService, new PromptCommandRequest { Prompt = prompt, Model = null, Settings = _settings });
        }

        public async Task<string> StringChoice(string prompt, List<string> choices, string? model = null, string? instruction = null)
        {
            var command = _promptsFactory.GetCommand<StringChoiceCommand, string>("string-choice-resp", instruction);

            var response = await command.Prompt(_ollamaService, new StringChoiceRequest { Prompt = prompt, Choices = choices, Settings = null, Model = model });

            return response;
        }

        public async Task<List<string>> MultiChoice(string prompt, List<string> choices, int maxChoices, string? instruction = null)
        {
            var command = _promptsFactory.GetCommand<MultiChoiceCommand, List<string>>("multi-choice-resp",instruction);

            var response = await command.Prompt(_ollamaService, new MultiChoiceRequest { Prompt = prompt, Choices = choices, MaxSelections = maxChoices, Settings = _settings });
            
            return response;
        }

        //public async Task<ChatPrompt> ScoredStringChoice(string prompt, List<string> choices, string? model, int maxChoices, string? instruction = null)
        //{
        //    var command = _promptsFactory.GetCommand<ScoredChoiceCommand, ScoredStringChoice>("secored-choice-resp", instruction);

        //    var response = await command.Prompt(_ollamaService, new StringChoiceRequest { Prompt = prompt, Choices = choices, Settings = null, Model = model });

        //    return response;
        //}

        public async Task<TResult> GuidedPromptCommand<TCommand, TRequest, TResult>(TRequest request, string? instruction = null)
            where TCommand : BasePromptCommand<TResult>, new()
            where TRequest : PromptCommandRequest
                => await _promptsFactory.GetCommand<TCommand, TResult>(instruction).Prompt(_ollamaService, request);

        public async Task<TResult> DbPromptCommand<TCommand, TRequest, TResult>(TRequest request, string dbInstructionName, string? instruction = null)
            where TCommand : ChromaPromptCommand<TResult>, new()
            where TRequest : PromptCommandRequest
             => await _promptsFactory.GetCommand<TCommand, TResult>(dbInstructionName, instruction).Prompt(_ollamaService, request);
    }
}
