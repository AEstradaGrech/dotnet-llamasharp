namespace DotnetLlamaSharp.Models.Response
{
    public class ScoredChoiceDto
    {
        public string Choice { get; set; }
        public float Confidence { get; set; }
        public string Comment { get; set; }
    }
}
