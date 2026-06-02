using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using OllamaSharp.Models;

namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IOllamaSharpService
    {
        Task<ChatPrompt> SimplePrompt(SimpleCommandRequest request);
        Task<ChatPrompt> ChatPrompt(CommandChatRequest request, bool bWithSysmsgUpdate = false);
        Task<EmbedResponse> GetOllamaClientEmbeddings(EmbedRequest request);
    }
}
