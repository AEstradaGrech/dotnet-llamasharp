using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Request;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public static class FluentChainExtensions
    {
        public static IChaineable StartWith(IJsoneable command, PromptCommandRequest request, string? feedFwdInstruction = null)
            => Activator.CreateInstance(typeof(ChainStep), command, request, feedFwdInstruction) as IChaineable;
        
        public static IChaineable Then(this IChaineable step, IJsoneable command, PromptCommandRequest request, string? feedFwdInstruction = null)
        {
            var nextStep = step.ExpandTo(command, request);

            step.Link(nextStep, isForward: true, isTwoWay: true); // feedForwardData?

            return nextStep;
        }

        public static IChaineable ThenExecute(this IChaineable step, out ChainResult result, bool withFinalMessage = true)
        {
            result = step.ExecuteChainAsync(withFinalMessage)
                .ConfigureAwait(continueOnCapturedContext: false)
                .GetAwaiter()
                .GetResult();

            return step;
        }

        public static async Task<ChainResult> ThenExecuteAsync(this IChaineable step, bool withFinalMessage = true)
            => await step.ExecuteChainAsync(withFinalMessage); 
        

        public static IChaineable Then<TCommand, TResult>(this IChaineable step, string? instruction, PromptCommandRequest request, string? feedFwdInstruction = null) 
            where TCommand : BasePromptCommand<TResult>, new() 
            where TResult : class
        {
            var nextStep = step.ExpandTo<TCommand, TResult>(instruction, request);

            step.Link(nextStep, isForward: true, isTwoWay: true);

            return nextStep;
        }

        //public static ChainStep Then<TCommand, TResult>(this ChainStep step, PromptCommandRequest request,  PromptCommandRequest  commandRequest)
        //{
        //    //step.From<TCommand, typeof(PromptCommandRequest), 

        //    return step;
        //}
        public static TypedOutput<TResult> Then<TResult>(this TypedOutput<TResult> step, IPrompteable<TResult> cmd /*IPromptCommand<TResult> cmd*/, PromptCommandRequest request) where TResult : class
        {
           
            return step;
            //return nextStep;
        }
        //public static ChainStep LoadNext<TCommand>(this ChainStep step)
        //{

        //    return step;
        //}

        //public static ChatMessage ThenFinish(this ChainStep step)
        //{
        //    if (step.Command.GetType() == typeof(MessagePromptCommand)) return step.Output.TypedResult() as ChatMessage;

        //    return null;
        //}
    }
}
