

using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class JunctionStep : SingleThrowStep
    {
        public JunctionStep() : base() { }
        public JunctionStep(IJsoneable junctionCommand, PromptCommandRequest request, string? feedFwdMessage = null) : base(junctionCommand, request, feedFwdMessage) { }
        // NO ES el primer eslabon de una cadena (es un colector)
        // El anterior step es MULTI SOCKET
        // Tiene un comando que ejecutar (en este caso deberia validarse que _commands == 1 porque la pieza es N a 1 siempre
        public override bool CanBeForged(IChaineable previous)
            => previous != null && previous.IsMultiSocket && _commands.Count > 0;

        public override async Task<IChaineable> Forge(IChaineable previous)
        {
            checkCanForge(previous);

            if (!previous.IsMultiSocket)
                throw new InvalidOperationException($"{nameof(SplitterStep)} >> BAD CHAIN CONFIGURATION >> PREVIOUS STEP IS NOT MULTISOCKET >> A JunctionStep can only be connected from a SplitterStep (or subclasses of)");

            var castedPrev = (SplitterStep)previous;

            //splitter con splitters
            // BUILD MULTISOCKET MESSAGE
            //output es una unidad parentInstruct-instruction-guidance
            //TEMPLATE:

            //  original.instruction (OBJETIVO GENERAL) + SplitterStep._feedForwardMessage (puede llevar una instruccion general / descripcion del step
            //      N * output instruction & schema & result & guidance (RESULTADOS Y QUE DEBERIA HACERSE CON ELLO)
            var outputs = previous.GrouppedOutputs();

            var sb = new StringBuilder();

            // build a multi-socket result containing multi and single socket sub-chain results
            // Difference: a multi-socket uses the FirstStep.CommandInstruction AND .FeedForwardInstruction (if any) as general goal description for all the LAST step splitter results
            //             then each output has it's own instruction & specific-feedFwd guidance message
            //             Otherwise it is treated as a SingleOutput command (whether it is the first one or the last one)
            foreach(var key in outputs.Keys)
            {
                var forgedPrevious = castedPrev.GetForgedSubStep(key);

                // Build a multi-socket message result (Join after Tap | Split)
                if (outputs[key].Count > 1)
                {
                    var originalStep = castedPrev.GetPluggedById(key);

                    sb.Append($"> GOAL: ")
                      .Append(originalStep.PromptedInstruction)
                      .AppendLine()
                      .AppendLine(string.IsNullOrEmpty(originalStep.FeedForwardInstruction) ? string.Empty : $"> FURTHER INSTRUCTIONS: {originalStep.FeedForwardInstruction}");

                    outputs[key].ForEach(output =>
                    {
                        // BUILD MESSAGE
                        sb.AppendLine()
                          .AppendLine(guidanceMessageFrom(_request.Prompt, forgedPrevious.PromptedInstruction, output.SerializedResult, output.SchemaForMessage(), output.GuidanceMessage).Trim());

                        _instructionsLog.AddRange(previous.InstructionsLog);
                    });
                }
                else // build a single output result (Join after SPST | Pipe
                {
                    var output = outputs[key].FirstOrDefault();

                    sb.AppendLine()
                      .AppendLine(guidanceMessageFrom(_request.Prompt, forgedPrevious.PromptedInstruction, output.SerializedResult, output.SchemaForMessage(), output.GuidanceMessage).Trim());
                }
            }

            _request.GuidanceMessage = sb.ToString().Trim();
            
            await forgeLink(); // TODO: throw new LameChainException("AN ERROR WHILE PROMPTING REQ")

            return _next != null ? await _next.Forge(this) : this;
        }
    }
}
