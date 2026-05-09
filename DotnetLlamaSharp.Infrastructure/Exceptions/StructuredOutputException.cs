namespace DotnetLlamaSharp.Infrastructure.Exceptions
{
    public class StructuredOutputException : HttpException
    {
        private const string _message = "Invalid Structured Output response >> VALIDATION FAIL >> REASON: <REASON> >> CONFIDENCE SCORE: <SCORE>";
        public StructuredOutputException(string reason, float confidence) : base(_message.Replace("<REASON>", reason).Replace("<SCORE>", $"{confidence}")) {}
    }
}
