using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models;
using System.Text.Json;
using System.Text.Json.Schema;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases
{
    public abstract class BasePromptCommand<T> : IPrompteable<T>, IJsoneable
    {
        protected string? _systemMessage = null;
        protected IOllamaInferenceService _ollama = null;
        protected CommandSettings _settings = null;
        public string? SystemMessage => _systemMessage;

        public IOllamaInferenceService BorrowLlama => _ollama;

        public BasePromptCommand() { _settings = new CommandSettings(maxTokens: 600, temperature: 0f, topP: .1f, topK: 10); }
        public BasePromptCommand(IOllamaInferenceService ollama) : this() { _ollama = ollama; }
        public BasePromptCommand(IOllamaInferenceService ollama, string? systemMessage = null, CommandSettings? settings = null) : this(ollama)
        {
            _settings = settings ?? new CommandSettings();
            _systemMessage = systemMessage;
        }

        public abstract Task<T> Prompt(PromptCommandRequest request);
        public virtual Task<T> PromptSync(PromptCommandRequest request) { throw new NotImplementedException("This method is meant to be overriden whenever required"); }
        protected virtual async Task<string> getPromptInstruction(string? guidanceMessage = null) => string.IsNullOrEmpty(guidanceMessage) ? _systemMessage : $"{_systemMessage}\n\n{guidanceMessage}";
        
        
        protected void validateInputRequest<TReq>(PromptCommandRequest request) where TReq : PromptCommandRequest
        {
            if (request.GetType() != typeof(TReq))
                throw new InvalidOperationException($"{nameof(BasePromptCommand<T>)} >> request of type {request.GetType().Name} is not of type {typeof(TReq)}");
        }
        protected async Task<GenerateRequest> getGenerateRequest(PromptCommandRequest request, bool withInstruction = true)
            => new GenerateRequest
            {
                Model = string.IsNullOrEmpty(request.Model) ? "qwen2.5:7b" : request.Model,
                Prompt = request.Prompt,
                System = withInstruction ? await getPromptInstruction(request.GuidanceMessage) : string.Empty,
                Stream = false,
                Options = _settings.ToOllamaRequest()
            };
       
        public async Task<JsonPromptResult> JsonPrompt(PromptCommandRequest request, bool returnFullInstruction= false, string? preInstruction = null)
        {
            if (!string.IsNullOrEmpty(preInstruction))
                _systemMessage = $"{preInstruction} {_systemMessage}";

            var promptInstruction = await getPromptInstruction(returnFullInstruction ? request.GuidanceMessage : null);

            var commandPrompt = await Prompt(request);

            return new JsonPromptResult(promptInstruction, commandPrompt, commandPrompt.GetType(), JsonSerializer.Serialize(commandPrompt), JsonSerializerOptions.Default.GetJsonSchemaAsNode(commandPrompt.GetType()));
        }

        // Validators with defaultInstruction & no guidanceMessage
        protected virtual JsonOutputRefinerCommand<T> validatorFor<T>() where T : class => new JsonOutputRefinerCommand<T>(_ollama, null, _settings);
    }
}
