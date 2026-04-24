using DotnetLlamaSharp.Models.Api.Prompting.Request;
using DotnetLlamaSharp.Models.Api.Prompting.Response;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Services.Inference.Interfaces
{
    public interface IOllamaSharpService
    {
        Task<ChatPromptResponse> SimplePrompt(ChatPromptRequest request);
        IAsyncEnumerable<GenerateResponseStream?> SimplePromptStream(ChatPromptRequest request);
        Task<ChatPromptResponse> ChatPrompt(ChatPromptRequest request, bool bWithSysmsgUpdate = false);
        IAsyncEnumerable<ChatResponseStream?> ChatPromptStream(ChatPromptRequest request, bool bWithSysmsgUpdate = false);
    }
}
