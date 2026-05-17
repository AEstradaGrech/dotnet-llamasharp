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
        List<IChaineable> _branches = new List<IChaineable>();
        Dictionary<IChaineable, Guid> _forgedSubSteps;
        public SplitterStep(List<StepInstruction> instructions, PromptCommandRequest request, string? feedFwdMessage) : base(request, feedFwdMessage)
        {
            _forgedSubSteps = new Dictionary<IChaineable, Guid>();
            instructions.ForEach(i => _branches.Add(ExpandTo(i.Command, request, i.FeedFwdInstruction)));
        }

        // It is not the first step of a chain (prev != null)
        // The previous is a SPST
        // It has at leas one command to run (should assert it has at least two probably otherwise is a bad / stupid configuration)
        public override bool CanBeForged(IChaineable previous)
            => previous != null && !previous.IsMultiSocket && _commands.Count > 0;

        //public void Sink([] --> branches = array, isReversed = true (N - 1)
        //public void Split([] -> branches = array, isReversed = false (1 - N)
        public void Plug(IChaineable branch)
        {
            _branches.Add(branch);
        }

        public override async Task<IChaineable> Forge(IChaineable previous) //En el momento que hago split tipado hace que solo pueda recibir SplitterStep hasta .Join()
        {
            if (previous.IsMultiSocket)
                throw new InvalidOperationException($"{nameof(SplitterStep)} >> BAD CHAIN CONFIGURATION >> PREVIOUS STEP IS MULTISOCKET >> A SplitterStep can only be connected from a SingleThrowStep");


            if (_next == null)
                throw new InvalidOperationException($"{nameof(SplitterStep)} >> BAD CHAIN CONFIGURATION >> A CHAIN cannot end up in a SplitterStep and the current has no NEXT assigned >> CHAIN CANNOTE BE CLOSED");

            var chainResults = new List<IChaineable>();

            /*
             // ❌ This has a closure bug - all tasks capture the SAME 'branch' reference
                    foreach(var branch in _branches)
                        splitterTasks.Add(Task.Run(() => branch.Forge(previous)));

            // ✅ Fixed version:
                    foreach(var branch in _branches)
                    {
                        var currentBranch = branch;  // Capture by value
                        splitterTasks.Add(Task.Run(() => currentBranch.Forge(previous)));
                    }
            // ✅ This is safe - LINQ's Select() creates a new scope per item
             */
            var results = await Task.WhenAll(_branches.Select(branch => Task.Run(() => branch.Forge(previous))));

            results.ToList().ForEach(chaineable =>
            {
                var fistro = chaineable.GetFirstStep();
                _forgedSubSteps.Add(chaineable, fistro.Id);
                //var original = _branches.SingleOrDefault(b => b.Id == fistro.Id);
                // _branches.IndexOf(original);
                // _dictionary<int, Links> -> _dict[idx].AddRange(fistro.Outputs)

                // Y ya tengo relacionados _branchStep -> resultingStep -> resultingStep N outputs
                // Y se quedan como dictionary en Splitter a modo de backup
                // Y se añaden a Outputs, que es la interfaz comun a todos los steps O previous.GetOutputs() devuelve Outputs.First() [SPST] Outputs.All [SPLITTER w/N x SPST] Outputs.ByPlugged() SPLITTER w/N x N]?
                //                                                                     LO QUE TENGO ES QUE AGRUPAR POR ICHAINEABLE PARA GENERAR SECCIONES EN EL CURRENT SYS <- SECCION STEP-FWD_MSG-LINK(s) (1-1-N)
                
                //                                                                     ejemplo mas enrevesado: N Chainables w/ N LINKS cada uno (un Splitter con Splitters y SingleThrows)
                
                //                                                                      -------- se supone que el tema es montar sysmessages con PREVOUTS --------
                //
                //                                                                      A --- Afwd: 'create char cmd.tap(self faction, gen-profile) [TAP 2]  <-- splitter feedFwdMessage (parent. explica objetivo step gral)
                //                                                                        > A-Ln1 faction res --> [cada uno puede tener su propio fwdMessage)<-- child feedFwdMessage. explica objectivo child step especifico)
                //                                                                        > A-Ln2 profile res
                //                                                                      B --- Bfwd: 'gen-char-routines' [SPST]
                //                                                                        > B-Ln1
                //                                                                      C --- Cfwd: 'select game traits mood and personalities [Tap 3]
                //                                                                        > C-Ln1 : traits res
                //                                                                        > C-Ln2 : mood res
                //                                                                        > C-Ln3 : personalities res
                //
                // TODO ESTO ES PARA JOIN | PIPE, SPST NO PUEDE RECIBIR SPLITTERS:
                // override Forge(previous)
                //      if(previous.IsMultiSocket)
                //          Get as 1 (JOIN) | Get for PIPE = (1 command x prev CHAINEABLE (w/N opts) <- cmd sys = pregv.GroupBy(chaineableid).Foreach().BuildSectionFromOutputs().Then() -> command.JsonPrompt
                //                                    JOIN = 1 command with 1 section grouping all PREV OUT LINKS grouped by PREV CHAINEABLE ID (el esquema de arriba) --> produce 1 Output
                //  previous.Outputs.GroupBy(link => link.StepId).ToList().ForEach(group => { previous.Get}
                //cada IChaineable puede devolver N outputs. No tienen porque ser subcadenas cerradas ni terminadas en SingleThrow
                // el chaineable ya NO es el mismo objeto que en BRANCHES (ID != ID)
                Outputs.AddRange(chaineable.Outputs);
                // 17-05-26: como funciona >> cada Branch ejecuta su subchain. puede acabar en splitter y devolver N results por Branch.
                //                            cada result esta marcado con el GUID del comando que lo ejecutó asi que se guardan juntos y revueltos en base.Outputs
                //                            cuando llega el siguiente se pillan los previous.GrouppedOutputs() = Dictionary<Guid, List<ChainLink>> [por cada branch N output]
                //                            si necesito recuperar la branch que los ejecuto... el OUTPUT TIENE EL ID DEL ULTIMO ESLABON
                //                            PARA QUE QUIERO EL PRIMER ESLABON DE LA SUB CHAIN --> poque el patron de arriba es recursivo: N results = parent feedFwd (primerSlabon) + child res feedFwd (ultimo Eslabon)
                //                              PARA RECUPERAR LA INSTRUCCION DE LA SUBCADENA Y MONTAR EL MENSAJE DEL SIGUIENTE
                //                                   (es decir, lleva Instruction (FIRST STEP) + FeedFwd (LAST STEP) >> PREVIOUS_INSTRUCTION + >> EXPECTED TO DO WITH (last) OUTPUT)
                //  NUEVA REGLA: el FeedFwd que cuenta en una subchain es el ultimo, y punto (primero guia a segundo etc, no aporta nada en resultado final)
                // tendria que reconstruir desde stepId hasta firstStep ha que guardar entonces los CHAINEABLES DE VUELTA
                _instructionsLog.Add($"- SUB CHAIN {chaineable.Id} LOG:");
                _instructionsLog.AddRange(chaineable.InstructionsLog);
            });   

            return await _next.Forge(this);
        }

        public IChaineable GetPluggedById(Guid stepId)
        {
            if (!_branches.Any(step => step.Id == stepId))
            {
                return _forgedSubSteps.Keys.Any(key => key.Id == stepId) ?
                    _branches.SingleOrDefault(branch => branch.Id == _forgedSubSteps.Select(kvp => kvp).Where(kvp => kvp.Key.Id == stepId).Select(x => x.Value).First()) :
                    null;
            }

            else return _branches.SingleOrDefault(step => step.Id == stepId);
        }

        public IChaineable GetForgedSubStep(Guid stepId)
            => _forgedSubSteps.Keys.FirstOrDefault(step => step.Id == stepId);
    }
}
