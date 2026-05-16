using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Services.Inference;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases
{
    public interface IJsoneable
    {
        Task<JsonPromptResult> JsonPrompt(PromptCommandRequest request);
    }
}
