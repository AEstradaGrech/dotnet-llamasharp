using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases
{
    public abstract class BasePromptCommand<T>
    {

        protected string? _systemMessage = null;
        protected CommandSettings _settings = null;
        public string? SystemMessage => _systemMessage;
        
        public BasePromptCommand() { _settings = new CommandSettings(maxTokens: 600, temperature: 0f, topP: .1f, topK: 10); }
        public BasePromptCommand(string? systemMessage = null, CommandSettings? settings = null) : this()
        {
            _settings = settings ?? new CommandSettings();
            _systemMessage = systemMessage;
        }

        public abstract Task<T> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request);
        public virtual Task<T> PromptSync(IOllamaInferenceService ollama, PromptCommandRequest request) { throw new NotImplementedException("This method is meant to be overriden whenever required"); }
        protected abstract Task<string> getPromptInstruction();
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
                System = withInstruction ? await getPromptInstruction() : string.Empty,
                Stream = false,
                Options = requestSettings()
            };
        protected RequestOptions requestSettings()
            => new RequestOptions {
                NumCtx = _settings.ContextLength ?? 4096,
                NumPredict = _settings.MaxTokens ?? 300,
                Temperature = _settings.Temperature ?? 0f,
                TopP = _settings.TopP ?? .1f,
                TopK = _settings.TopK ?? 10,
                RepeatPenalty = _settings.RepeatPenalty,
                RepeatLastN = _settings.RepeatLastN,
                MiroStat = _settings.MiroStat,
                MiroStatEta = _settings.MiroStatEta,
                MiroStatTau = _settings.MiroStatTau,
            };
        
    }
}
