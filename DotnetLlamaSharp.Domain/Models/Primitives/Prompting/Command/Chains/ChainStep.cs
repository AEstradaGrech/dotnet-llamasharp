using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class ChainStep : IChaineable
    {
        private IPromptCommand _command;
        private IPromptCommand _next;
        public IPromptCommand Command => _command;
        public IPromptCommand Next => _next;

        public void Chain(IPromptCommand next)
        {
            _next = next;
        }

        public bool IsChained()
            => Next != null;

        
    }
}
