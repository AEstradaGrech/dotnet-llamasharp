namespace DotnetLlamaSharp.Models.Response.Prompting.Commands
{
    public class ScoredChoiceDto
    {
        public string Choice { get; set; }
        public float Confidence { get; set; }
        public string Comment { get; set; }
    }
}
