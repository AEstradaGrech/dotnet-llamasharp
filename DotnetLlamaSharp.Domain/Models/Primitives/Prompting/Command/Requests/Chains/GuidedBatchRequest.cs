using DotnetLlamaSharp.Domain.Models.Request;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests.Chains
{
    public class GuidedBatchRequest : SimpleCommandRequest
    {
        public List<string> Instructions { get; set; } = new List<string>();
    }
}
