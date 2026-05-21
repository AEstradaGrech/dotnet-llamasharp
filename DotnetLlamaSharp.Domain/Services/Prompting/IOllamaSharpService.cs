using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests.Chains;
using DotnetLlamaSharp.Domain.Models.Request;
using OllamaSharp.Models;

namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IOllamaSharpService
    {
        Task<ChatPrompt> SimplePrompt(SimplePromptRequest request);
        Task<ChatPrompt> ChatPrompt(ChatPromptRequest request, bool bWithSysmsgUpdate = false);
        Task<EmbedResponse> GetOllamaClientEmbeddings(EmbedRequest request);
        Task<ChatPrompt> GuidedBatchChainPrompt(ChainTestRequest request);
        Task<ChainResult> GuidedBatchChain(ChainTestRequest request);
        //Task<ChatPrompt> BooleanQuestion(SimplePromptRequest request);
    }
}
