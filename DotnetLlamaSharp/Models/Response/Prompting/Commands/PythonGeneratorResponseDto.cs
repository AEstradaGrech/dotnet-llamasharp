namespace DotnetLlamaSharp.Models.Response.Prompting.Commands
{
    public class PythonGeneratorResponseDto
    {
        public string Code { get; set; }
        public List<string> Dependencies { get; set; }
    }
}
