using DocumentFormat.OpenXml.Office2010.CustomUI;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Services.Inference;
using DotnetLlamaSharp.Domain.Services.Prompting;
using DotnetLlamaSharp.Infrastructure.Exceptions;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Text.Json;
using System.Text.Json.Schema;


namespace DotnetLlamaSharp.Infrastructure.Services.Inference
{
    public class OllamaInferenceService : IOllamaInferenceService
    {
        private readonly IOllamaApiClient _client;

        private readonly ApiSettings _apiSettings;
        private readonly RequestOptions _settings;
        private readonly ILogger<OllamaInferenceService> _logger;
        public OllamaInferenceService(IOllamaApiClient client, IOptions<ApiSettings> apiSettings, IOptions<RequestOptions> settings, ILogger<OllamaInferenceService> logger)
        {
            _client = client;
           
            _apiSettings = apiSettings.Value;
            _settings = settings.Value;
            _logger = logger;
        }
       
        public async Task<Message> GeneratePrompt(GenerateRequest request)
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

        public IAsyncEnumerable<GenerateResponseStream?> GeneratePromptStream(GenerateRequest request)
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

        public async Task<T> StructuredPrompt<T>(string prompt, string? systemGuidance = null, string? jsonModel = null/*validations = 0*/) where T : class
        {
            if (string.IsNullOrEmpty(prompt) && string.IsNullOrEmpty(systemGuidance))
                throw new InvalidDataException($"{nameof(OllamaInferenceService)} >> {nameof(StructuredPrompt)} >> no messages to send");

            var options = new RequestOptions { TopK = _apiSettings.JsonOutputTopK, TopP = _apiSettings.JsonOutputTopP, Temperature = _apiSettings.JsonOutputTemp };
            var request = new ChatRequest
            {
                Model = string.IsNullOrEmpty(jsonModel) ? _apiSettings.JsonModels[0] : _apiSettings.JsonModels.Contains(jsonModel) ? jsonModel : _apiSettings.JsonModels[0],
                Messages = !string.IsNullOrEmpty(systemGuidance) ? [new Message { Role = ChatRole.System, Content = systemGuidance }, new Message { Role = ChatRole.User, Content = prompt}] : [new Message { Role = ChatRole.User, Content = prompt }],
                Format = JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(T)),
                Stream = false
            };
            
            var sb = new System.Text.StringBuilder();
          
            await foreach (var part in _client.ChatAsync(request))
                if (!string.IsNullOrEmpty(part?.Message.Content))
                    sb.Append(part.Message.Content);

            return JsonSerializer.Deserialize<T>(sb.ToString());
        }

        public async Task<T> CommandPrompt<T>(GenerateRequest request, int validations = 0, EPromptValidation type = EPromptValidation.REVIEW_ONLY, JsonOutputRefinerCommand<T> validator = null) where T : class
        {
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < validations + 1; i++)
            {
                try
                {
                    if (string.IsNullOrEmpty(request.Prompt))
                        throw new InvalidDataException($"{nameof(OllamaInferenceService)} >> {nameof(StructuredPrompt)} >> no messages to send");

                    request.Format = JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(T));
                    request.Stream = false;

                    await foreach (var part in _client.GenerateAsync(request))
                        if (!string.IsNullOrEmpty(part?.Response))
                            sb.Append(part.Response);
                  
                    if (validations > 0 && validator != null)
                    {
                        //has no default | db message. Orchestrates commands with default | db message. uses the ChromaCommands FactoryMethod to get a ChromaRepo for the child commands
                        return validator.PromptSync(new JsonRefineRequest<T> { Prompt = request.Prompt, SystemMessage = request.System, ValidationType = type,  RawOutput = sb.ToString(), Settings = request.Options }).Result;
                    }
                }
                catch(StructuredOutputException ex)
                {
                    _logger.LogWarning(ex.Message);

                    if (i == validations)
                        throw new PromptRetryException($"{nameof(StructuredPrompt)} >> JSON OUTPUT VALIDATIONS LIMIT REACHED", retries: validations);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"{nameof(StructuredPrompt)} >> An error has occured while getting the Structured Output response >> EXCEPTION: {ex.ToString()}");

                    if (ex.GetType() == typeof(PromptRetryException))
                        throw ex;

                    if (i == validations)
                        throw new PromptRetryException($"{nameof(StructuredPrompt)} >> JSON OUTPUT VALIDATIONS LIMIT REACHED", retries: validations);
                }
            }

            return JsonSerializer.Deserialize<T>(sb.ToString());
        }
    }
}
