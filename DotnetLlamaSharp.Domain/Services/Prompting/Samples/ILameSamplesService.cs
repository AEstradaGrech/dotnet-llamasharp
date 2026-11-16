using Dotnet.OllamaSharp.LameChain.SDK.Commands.Base;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Response.StructuredOutputs;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Interfaces.Model;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Request;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Response;
using DotnetLlamaSharp.Domain.Models.Request.Prompting.Commands;

namespace DotnetLlamaSharp.Domain.Services.Prompting.Samples
{
    /// <summary>
    /// Interface for the LameChain Samples service that provides usage examples for
    /// LameChain Commands and usage of Commands with LameChainExtensions
    /// </summary>
    public interface ILameSamplesService
    {
        Task<ChainResult> ParallelChainExample(ChainedPrompt request);
        Task<ChainResult> ParallelSubChainsExample(ChainedPrompt request);
        Task<ChainResult> SmartRagChain(SimpleCommandRequest request);
        Task<ChainResult> ConditionalChatChain(CommandChatRequest request);
        Task<ChatMessage> GuidedPromptCommand(SimpleCommandRequest request);
        Task<ChatMessage> DbPromptCommand(SimpleCommandRequest request);
        Task<ChatMessage> DbPromptCommandDefaultMode(SimpleCommandRequest request);
        Task<List<ILameSearchResult>> VectorSearchCommand(VectorSearchRequest request);
        Task<List<string>> VectorSearchSourceable(VectorSearchRequest request);
        StoreableCommand<TStored> StoreableCommand<TStored>(Func<TStored, string, Task<TStored>> storingLambda) where TStored : class;

        Task<PythonGeneratorResponse> PythonGeneratorCommand(SimpleCommandRequest request);
        Task<ChatMessage> PythonExecutorCommand(PythonCodingCommandRequest request);
        Task<ChatMessage> PythonCodingChain(SimpleCommandRequest request);
        Task<ChatMessage> PythonCodingConditionalChain(SimpleCommandRequest request);

        Task<ScriptGeneratorResponse> ScriptGeneratorCommand(ScriptGeneratorRequest request);
        Task<ChatMessage> ScriptExecutorCommand(ScriptExecutorRequest request);
        Task<string> AgentPromptSample_ToolsAndSkill(string model, string prompt, string? name, string? description, PromptSettings? settings, string? instruction = null);
        Task<string> AgentPromptSample_ToolsFromSkills(string model, string prompt, string? name, string? description, PromptSettings? settings, string? instruction = null);
        Task<string> AgentPromptSample_ChromaAgent(string model, string prompt, string? name, string? description, PromptSettings? settings, string? instruction = null);
        Task<string> AgentPromptSample_SelectedSkillsAgent(string model, string prompt, string? name, string? description, PromptSettings? settings, string? instruction = null);
        Task<string> AgentPromptSample_AutoSkilledAgent(string model, string prompt, string? name, string? description, PromptSettings? settings, string? instruction = null);
        Task<string> AgentPromptSample_PythonCodingAgent(string model, string prompt, PromptSettings? settings = null, CommandSettings? toolSettings = null);
        Task<string> AgentPromptSample_SysAdminAgent(string model, string prompt, PromptSettings? settings = null);
        Task<string> AgentPromptSample_PhilosopherAgent(string model, string prompt, string? systemMessage = null, PromptSettings? settings = null);
        Task<string> AgentPromptSample_SmartPhilosopherAgent(string model, string prompt, string? systemMessage = null, PromptSettings? settings = null);
    }
}
