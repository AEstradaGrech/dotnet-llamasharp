using OllamaSharp.Models;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class ChainPromptRequest : PromptCommandRequest
    {
        public ChainPromptRequest() 
        {
            ForwardChainFeeds = new List<Guid>();
            ForwardStringFeeds = new List<string>();
        }
        public ChainPromptRequest(string message, string? guidanceMessage, CommandSettings? settings, string? model = null) 
            : base(message, guidanceMessage,settings != null ? settings.ToOllamaRequest() : new RequestOptions(), model) 
        {
            ForwardChainFeeds = new List<Guid>();
            ForwardStringFeeds = new List<string>();
        }

        public List<Guid> ForwardChainFeeds { get; set; } // Taps de la cadena
        
        public List<string> ForwardStringFeeds { get; set;  } // es lo mismo ya es pasarse pero es para meter feed de sources que no son steps

        public CommandSettings? StepSettings { get; set; } = null; // ?? _runner.DefaultSettings (from userReq)
        public ChainPromptRequest FeedFrom(Guid id)
        {
            if (!ForwardChainFeeds.Contains(id))
                ForwardChainFeeds.Add(id);

            return this;
        }
    }
}
