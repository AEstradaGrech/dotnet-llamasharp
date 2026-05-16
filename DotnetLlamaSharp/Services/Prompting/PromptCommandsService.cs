using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Services.Inference;
using DotnetLlamaSharp.Domain.Services.Prompting;
using Microsoft.Extensions.Options;
using OllamaSharp.Models;

namespace DotnetLlamaSharp.Services.Prompting
{
    public class PromptCommandsService : IPromptCommandsService
    {
        private readonly IPromptCommandsFactory _factory;
        private readonly RequestOptions _settings;
        public PromptCommandsService(IPromptCommandsFactory factory,   IOptions<RequestOptions> settings)
        {
            _factory = factory;
            _settings = settings.Value;
        }

        public async Task<TResult> GuidedPromptCommand<TRequest, TResult>(TRequest request, string? instruction = null, CommandSettings settings = null)
            where TRequest : PromptCommandRequest
            where TResult : class
                => await _factory.GetCommand<GuidedStructuredPrompt<TResult>, TResult>(instruction, settings).Prompt(request);

        //Any subclass or custom type of GuidedStructuredCommand
        public async Task<TResult> GuidedPromptCommand<TCommand, TRequest, TResult>(TRequest request, string? instruction = null, CommandSettings settings = null)
            where TCommand : BasePromptCommand<TResult>, new()
            where TRequest : PromptCommandRequest
                => await _factory.GetCommand<TCommand, TResult>(instruction, settings).Prompt(request);

        public async Task<TResult> DbPromptCommand<TRequest, TResult>(TRequest request, string dbInstructionName, string? instruction = null, CommandSettings settings = null)
           where TRequest : PromptCommandRequest
           where TResult : class
               => await _factory.GetCommand<ChromaStructuredPrompt<TResult>, TResult>(dbInstructionName, instruction, settings).Prompt( request);

        // All subclasses or custom types that should work as a DbCommand (have a message name and a IChromaRepo instance)
        public async Task<TResult> DbPromptCommand<TCommand, TRequest, TResult>(TRequest request, string dbInstructionName, string? instruction = null, CommandSettings settings = null)
            where TCommand : DbPromptCommand<TResult>, new()
            where TRequest : PromptCommandRequest
                => await _factory.GetCommand<TCommand, TResult>(dbInstructionName, instruction, settings).Prompt(request);

        //TODO: DefaultMessageVersion for:
        public async Task<TEnum?> EnumChoice<TEnum>(string prompt, string? instruction = null, CommandSettings settings = null) where TEnum : struct, Enum
        {
            var command = _factory.GetEnumChoiceCommand<TEnum>("enum-choice-resp", instruction, settings);

            return await command.Prompt(new PromptCommandRequest(prompt, _settings, settings.Model));
        }

        public async Task<string> StringChoice(string prompt, List<string> choices, string? instruction = null, CommandSettings settings = null)
        {
            var command = _factory.GetCommand<StringChoiceCommand, string>("string-choice-resp", instruction, settings);

            var response = await command.Prompt(new StringChoiceRequest(choices, prompt, _settings,  settings.Model));

            return response;
        }

        public async Task<List<string>> MultiChoice(string prompt, List<string> choices, int maxChoices, string? instruction = null, CommandSettings settings = null)
        {
            var command = _factory.GetCommand<MultiChoiceCommand, List<string>>("multi-choice-resp", instruction, settings);

            var response = await command.Prompt(new MultiChoiceRequest(maxChoices, choices, prompt, _settings, settings.Model));

            return response;
        }

        public async Task<bool> BooleanChoice(string prompt, string? instruction = null, CommandSettings settings = null)
        {
            var command = _factory.GetCommand<BoolPromptCommand, bool>("bool-resp", instruction, settings);

            return await command.Prompt(new PromptCommandRequest(prompt, _settings, settings.Model));
        }

        public async Task<string> StringBoolChoice(string prompt, string? instruction = null, CommandSettings settings = null)
            => await BooleanChoice(prompt, instruction, settings) ? "YES" : "NO";

        public async Task<float?> NumericResult(string prompt, string? instruction = null, CommandSettings settings = null)
        {
            var command = _factory.GetNumericPromptCommand("numeric-resp", instruction, settings);

            return await command.Prompt(new PromptCommandRequest(prompt, _settings, settings.Model));
        }

        public async Task<ScoredBoolResponse> ScoredBool(string prompt, string? instruction = null, CommandSettings settings = null)
            => await DbPromptCommand<ScoredBoolCommand, PromptCommandRequest, ScoredBoolResponse>(
                new PromptCommandRequest(prompt, _settings, settings.Model), dbInstructionName: "scored-bool-resp", instruction: instruction, settings);

        public async Task<ScoredStringChoice> ScoredChoice(List<string> choices, string prompt, string? instruction = null, CommandSettings settings = null)
            => await DbPromptCommand<ScoredChoiceCommand, StringChoiceRequest, ScoredStringChoice>(
                new StringChoiceRequest(choices, prompt, _settings, settings.Model), dbInstructionName: "scored-choice-resp", instruction: instruction, settings);

        public TCommand GetCommand<TCommand, TResult>(string dbMessageName, string? guidanceMessage = null, PromptSettings? settings = null) where TCommand : DbPromptCommand<TResult>, new()
                => _factory.GetCommand<TCommand, TResult>(dbMessageName, guidanceMessage, settings);
    }
}
