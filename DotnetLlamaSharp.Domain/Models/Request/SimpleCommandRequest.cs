using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class SimpleCommandRequest : BasePromptRequest
    {
        public CommandSettings Settings { get; set; }
    }
}
