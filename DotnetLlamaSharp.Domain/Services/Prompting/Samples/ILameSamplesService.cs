using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Interfaces.Model;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Request;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Response;
using DotnetLlamaSharp.Domain.Models.Request;

namespace DotnetLlamaSharp.Domain.Services.Prompting.Samples
{
    /// <summary>
    /// Interface for the LameChain Samples service that provides usage examples for
    /// LameChain Commands and usage of Commands with LameChainExtensions
    /// </summary>
    public interface ILameSamplesService
    {
        Task<ChainResult> ParallelChainExample(ChainedPrompt request);
        Task<ChainResult> SmartRagChain(SimpleCommandRequest request);
        Task<ChainResult> ConditionalChatChain(CommandChatRequest request);
        Task<ChatMessage> GuidedPromptCommand(SimpleCommandRequest request);
        Task<ChatMessage> DbPromptCommand(SimpleCommandRequest request);
        Task<ChatMessage> DbPromptCommandDefaultCompatible(SimpleCommandRequest request);
        Task<List<ILameSearchResult>> VectorSearchCommand(VectorSearchRequest request);
        Task<List<string>> VectorSearchSourceable(VectorSearchRequest request);
    }
}
