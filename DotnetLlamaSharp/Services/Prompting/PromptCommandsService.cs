using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Services.Inference;
using DotnetLlamaSharp.Domain.Services.Prompting;
using Microsoft.Extensions.Options;
using OllamaSharp.Models;

namespace DotnetLlamaSharp.Services.Prompting
{
    public class PromptCommandsService : IPromptCommandsService
    {
        private readonly IPromptCommandsFactory _factory;
        private readonly IOllamaInferenceService _ollama;
        private readonly RequestOptions _settings;
        public PromptCommandsService(IPromptCommandsFactory factory, IOllamaInferenceService ollama, IOptions<RequestOptions> settings)
        {
            _factory = factory;
            _ollama = ollama;
            _settings = settings.Value;
        }

        public async Task<TResult> GuidedPromptCommand<TCommand, TRequest, TResult>(TRequest request, string? instruction = null)
            where TCommand : BasePromptCommand<TResult>, new()
            where TRequest : PromptCommandRequest
                => await _factory.GetCommand<TCommand, TResult>(instruction).Prompt(_ollama, request);

        public async Task<TResult> DbPromptCommand<TCommand, TRequest, TResult>(TRequest request, string dbInstructionName, string? instruction = null)
            where TCommand : ChromaPromptCommand<TResult>, new()
            where TRequest : PromptCommandRequest
                => await _factory.GetCommand<TCommand, TResult>(dbInstructionName, instruction).Prompt(_ollama, request);

        public async Task<TEnum?> EnumChoice<TEnum>(string prompt, string? model = null, string? instruction = null) where TEnum : struct, Enum
        {
            var command = _factory.GetEnumChoiceCommand<TEnum>("enum-choice-resp", instruction);

            return await command.Prompt(_ollama, new PromptCommandRequest(prompt, _settings, model));
        }

        public async Task<string> StringChoice(string prompt, List<string> choices, string? model = null, string? instruction = null)
        {
            var command = _factory.GetCommand<StringChoiceCommand, string>("string-choice-resp", instruction);

            var response = await command.Prompt(_ollama, new StringChoiceRequest(choices, prompt, _settings,  model));

            return response;
        }

        public async Task<List<string>> MultiChoice(string prompt, List<string> choices, int maxChoices, string? model = null, string? instruction = null)
        {
            var command = _factory.GetCommand<MultiChoiceCommand, List<string>>("multi-choice-resp", instruction);

            var response = await command.Prompt(_ollama, new MultiChoiceRequest(maxChoices, choices, prompt, _settings, model));

            return response;
        }

        public async Task<bool> BooleanChoice(string prompt, string? model = null, string? instruction = null)
        {
            var command = _factory.GetCommand<BoolPromptCommand, bool>("bool-resp", instruction);

            return await command.Prompt(_ollama, new PromptCommandRequest(prompt, _settings, model));
        }

        public async Task<string> StringBoolChoice(string prompt, string? model = null, string? instruction = null)
            => await BooleanChoice(prompt, model, instruction) ? "YES" : "NO";

        public async Task<float?> NumericResult(string prompt, string? model = null, string? instruction = null)
        {
            var command = _factory.GetNumericPromptCommand("numeric-resp", instruction);

            return await command.Prompt(_ollama, new PromptCommandRequest(prompt, _settings, model));
        }
    }
}
