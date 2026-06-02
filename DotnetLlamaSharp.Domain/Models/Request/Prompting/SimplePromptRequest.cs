using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;

namespace DotnetLlamaSharp.Domain.Models.Request.Prompting
{
    /// <summary>
    /// Request without LameChain ddependency (to use with the Straeming Service or whenever you want to implement an ollama service without commands)
    /// </summary>
    public class SimplePromptRequest : BasePromptRequest
    {
        public PromptSettings Settings { get; set; }
    }
}
