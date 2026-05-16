using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Request;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class BufferedStep : ChainStep
    {
        public BufferedStep(IJsoneable command, PromptCommandRequest request, bool isPreloaded) : base(command, request, isPreloaded)
        {
        }

        public List<IJsoneable> LoadedCommands { get; set; }

        public void Load(IJsoneable command)
        {
            LoadedCommands.Add(command);
        }

        public ChatMessage Run()
        {
            Queue<IJsoneable> Q = new Queue<IJsoneable>();
            LoadedCommands.ForEach(cmd => Q.Enqueue(cmd));
            return null;
        }

        /// #WG : RunThen VS LoadNext 
        private void Execute(SimpleCommandRequest request, ChainStep initial, Queue<IJsoneable> q)
        {
            var firstCommand = q.Dequeue();

            var cmdReq = new PromptCommandRequest { Prompt = request.Prompt, Model = request.Settings.Model };

            IChaineable firstStep = new ChainStep(firstCommand, new PromptCommandRequest() /*TODO: simpleToPromptReq*/, isPreloaded: true); 

            var currentStep = firstStep;

            while (q.Count > 0)
            {
                currentStep = currentStep.Then(q.Dequeue(), cmdReq);

                if (q.Count == 0) break;
            }

            
        }
    }
}
