using Dotnet.OllamaSharp.LameChain.SDK.Models.Request;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Response;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using OllamaSharp.Models;

namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IOllamaSharpService
    {
        Task<ChatPrompt> SimplePrompt(SimplePromptRequest request);
        Task<ChatPrompt> ChatPrompt(ChatPromptRequest request, bool bWithSysmsgUpdate = false);
        Task<EmbedResponse> GetOllamaClientEmbeddings(EmbedRequest request);
        Task<ChatPrompt> GuidedBatchChainPrompt(ChainedPrompt request);
        Task<ChainResult> GuidedBatchChain(ChainedPrompt request);
    }
}
