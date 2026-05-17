using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
   //
   // Executes all branch commands using the output of the previous link
   // generates 1 link x branch
   // if you concat a .Then after a .Spli,t IT APPENDS EVERY PREV OUTPUT AND RETURS ONE LINK. .Pipe(this Splitter) executes THE COMMAND ON EACH PREV OUTPUT AND GENERATES 1 X PREV OUT LINKS
   //   .Join<TJoiner>() merges EVERY PREV OUT LINK USING A SPECIFIC COMMAND (as opposed to the .Then 'raw' appending)
   // OPCION B: SingleThrowStep & SplitterStep extensions --> .Then para SingleThrow, pipe para Splitter, asi queda mas cerrado el uso (hasta que no haces Join no puedes usar .Then
   //           y el merge a las bravas del .Split().Then() pasaria a ser un .Join sin TJoiner. Es decir, tendria .Join(nextCommand) (append burro = ejecuto next con todo lo que haya prev) y
   //                                                                                                             .Join<TJoiner>(nextCommand) que interiormente sequencia un reducer con el nextCommand)
   //                                                                                                                es decir, procesa el multioutput como quiere el usuario y ese es el feed de nextCommand
   // Split([]) (NO A B)

    public class SplitterStep : ChainStep
    {
        List<IChaineable> _branches;
        //dflt = 1 - N --> 1 prev , N next
        //rev = N - 1
        bool _isReversed;
        List<ChainLink> _links; //1 x branch

        public SplitterStep(List<StepInstruction> instructions, PromptCommandRequest request) : base(request)
        {
            instructions.ForEach(i => _branches.Add(ExpandTo(i.Command, request, i.FeedFwdInstruction)));
        }
        public override bool CanBeForged(IChaineable previous)
            => _commands.Count > 0 && !previous.IsMultiSocket;

        //public void Sink([] --> branches = array, isReversed = true (N - 1)
        //public void Split([] -> branches = array, isReversed = false (1 - N)
        public void Plug(IChaineable branch)
        {
            _branches.Add(branch);
        }
        public override async Task<IChaineable> Forge(IChaineable previous) //En el momento que hago split tipado hace que solo pueda recibir SplitterStep hasta .Join()
        {
            if(previous.IsMultiSocket)
            {
                //o tiene 1 output o tiene N
            }
            else
            {
                // he conectado un SPST a un Splitter
                // branches.Foreach(branch => {
                //  tasks.Add(TaskFactory.StartNew(() => { branch.JsonPrompt & add link})
                // })
                // await Tasks.WhenAll()
                // return _next o _prev | _nexts o _prevs
            }
            //isReversed

            // else:
            //  foreach chaineable.Task
            return null;
        }
    }
}
