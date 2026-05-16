using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Services.Inference;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases
{
    public interface IPrompteable<TResult>
    {
        Task<TResult> Prompt(PromptCommandRequest request);
        Task<TResult> PromptSync(PromptCommandRequest request);
    }
}
