using DotnetLlamaSharp.Domain.Models.Request;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;


namespace DotnetLlamaSharp.Domain.Services.Prompting
{
    public interface IOllamaStreamService
    {
        IAsyncEnumerable<GenerateResponseStream?> SimplePromptStream(SimplePromptRequest request);
        IAsyncEnumerable<ChatResponseStream?> ChatPromptStream(ChatPromptRequest request, bool bWithSysmsgUpdate = false);
    }
}
