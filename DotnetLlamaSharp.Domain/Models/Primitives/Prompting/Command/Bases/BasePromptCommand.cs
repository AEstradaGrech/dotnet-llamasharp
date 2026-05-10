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
            if (request.GetType() != typeof(JsonRefineRequest<TReq>))
                throw new InvalidOperationException($"{nameof(BasePromptCommand<T>)} >> request of type {request.GetType().Name} is not of type {typeof(TReq)}");
        }
        protected async Task<GenerateRequest> getGenerateRequest(PromptCommandRequest request, bool withInstruction = true)
            => new GenerateRequest
            {
                Model = request.Model,
                Prompt = request.Prompt,
                System = withInstruction ? await getPromptInstruction() : string.Empty,
                Stream = false,
                Options = requestSettings()
            };
        protected RequestOptions requestSettings()
            => new RequestOptions {
                NumCtx = _settings.ContextLength,
                NumPredict = _settings.MaxTokens,
                Temperature = _settings.Temperature,
                TopP = _settings.TopP,
                TopK = _settings.TopK,
                RepeatPenalty = _settings.RepeatPenalty,
                RepeatLastN = _settings.RepeatLastN,
                MiroStat = _settings.MiroStat,
                MiroStatEta = _settings.MiroStatEta,
                MiroStatTau = _settings.MiroStatTau,
            };
        
    }
}
