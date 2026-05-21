using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Request;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class BufferedStep : ChainStep
    {
        public BufferedStep() : base() { }
        public BufferedStep(IJsoneable command) : base() {}

        public List<IJsoneable> LoadedCommands { get; set; }

        public override bool CanBeForged(IChaineable previous)
        {
            throw new NotImplementedException();
        }

        public override Task<IChaineable> Forge(IChaineable previous)
        {
            throw new NotImplementedException();
        }

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
            //var firstCommand = q.Dequeue();

            //var cmdReq = new StepSettings { Prompt = request.Prompt, Model = request.Settings.Model };

            //var firstStep = new SingleThrowStep(firstCommand, new ChainRunner(userInput: request.Prompt,  request.Settings, finalSysMessage: request.SystemMessage)); 

            //var currentStep = firstStep;

            //while (q.Count > 0)
            //{
            //    currentStep = currentStep.Then(q.Dequeue(), cmdReq);

            //    if (q.Count == 0) break;
            //}

            
        }
    }
}
