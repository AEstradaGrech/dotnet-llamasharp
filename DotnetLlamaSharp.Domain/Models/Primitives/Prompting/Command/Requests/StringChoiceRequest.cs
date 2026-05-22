using OllamaSharp.Models;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class StringChoiceRequest : PromptCommandRequest
    {
        public StringChoiceRequest() { }
        public StringChoiceRequest(List<string> choices, string message, RequestOptions? settings = null, string? model = null) : base(message, settings, model)
        {
            Choices = choices;
        }
        public List<string> Choices { get; set; }
    }
}
