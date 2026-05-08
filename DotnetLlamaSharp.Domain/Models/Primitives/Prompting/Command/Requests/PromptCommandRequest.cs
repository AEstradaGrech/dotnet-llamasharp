using OllamaSharp.Models;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class PromptCommandRequest
    {
        public PromptCommandRequest() { }
        public PromptCommandRequest(string message, RequestOptions? settings, string? model = null) 
        {
            Prompt = message;
            Settings = settings;
            Model = model;

        }
        public string Prompt { get; set; }
        public string? Model { get; set; }
        public RequestOptions? Settings { get; set; }
    }
}
