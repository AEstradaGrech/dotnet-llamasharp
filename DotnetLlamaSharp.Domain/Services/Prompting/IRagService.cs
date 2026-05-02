using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;

namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IRagService
    {
        // Q&A
        Task<RagPrompt> SimpleRagQuery(RagPromptRequest request);
        // Chat Turns with rag
        Task<ChatPrompt> RagChatPrompt(RagChatRequest request);
    }
}
