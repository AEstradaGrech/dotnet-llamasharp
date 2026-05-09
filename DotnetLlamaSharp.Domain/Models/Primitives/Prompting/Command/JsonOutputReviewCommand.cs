using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Services.Inference;

//Reviews and Rewrites / corrects result
namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class JsonOutputReviewCommand<TReviewed> : BasePromptCommand<TReviewed>
    {
        public override Task<TReviewed> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            throw new NotImplementedException();
        }

        protected override Task<string> getPromptInstruction()
        {
            throw new NotImplementedException();
        }
    }
}
