
namespace DotnetLlamaSharp.Domain.Models.Request.Prompting.Commands
{
    public class ScriptExecutorRequest : SimpleCommandRequest
    {
        public required string ScriptPath { get; set; }
    }
}
