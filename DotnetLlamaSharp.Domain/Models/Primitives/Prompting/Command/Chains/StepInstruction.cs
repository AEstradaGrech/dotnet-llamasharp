using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class StepInstruction
    {
        public StepInstruction() { }
        public StepInstruction(IJsoneable command, StepSettings settings, string? feedFwd = null) 
        {
            Command = command;
            StepSettings = settings;
            FeedFwdInstruction = feedFwd;
        }
        public IJsoneable Command { get; }
        public StepSettings StepSettings { get; }
        public string? FeedFwdInstruction { get; }

        //TODO: FeedAlsoFrom[] recuperar results de eslabones mas alla del anterior
        //      - tambien recupera el FeedFwd de ese eslablon de esa forma se puede
        //          split(feed: 'do this with the results').Pipe(feedFwd: 'do also that' | null - skip).Join() <- FeedAlsoFrom([prev -1]) + prev FeedMsg (if any)
        //              > alimentar los inputs del pipe con el feedFwd del split y el Join (con o sin feedFwd del pipe)
        //          
        //          split(feed: 'do this with the results').Pipe().MoreStuff().Pipe(feedFwd: 'do also that' | null - skip).Join() <-FeedAlsoFrom([prev-x, prev-a, prev-n])
    }
}
