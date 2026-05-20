using OllamaSharp.Models;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class ChainPromptRequest : PromptCommandRequest
    {
        private const string _randomBoostTag = "RNDM_SET";
        private const string _boostersSectionTag = "## ADDITIONAL INFORMATION: "; 
        public ChainPromptRequest() 
        {
            ChainFeeds = new List<Guid>();
            Boosters = new List<KeyValuePair<string, List<string>>>();
        }
        public ChainPromptRequest(string message, string? guidanceMessage, CommandSettings? settings, string? model = null) 
            : base(message, guidanceMessage,settings != null ? settings.ToOllamaRequest() : new RequestOptions(), model) 
        {
            ChainFeeds = new List<Guid>();
            Boosters = new List<KeyValuePair<string, List<string>>>();
        }

        public List<Guid> ChainFeeds { get; set; } // Runner Ids to feed the context from
        
        public List<KeyValuePair<string,List<string>>> Boosters { get; set;  } // Additional sources of data to enhance the prompt, add few-shot examples or whatever This will be appended at the end of the system message as "#ADDITIONAL INFORMATION: <rebujito feed message to guide / interpret the feeds> 

        public CommandSettings? StepSettings { get; set; } = null; // ?? _runner.DefaultSettings (from userReq)
        public ChainPromptRequest FeedFrom(Guid id)
        {
            if (!ChainFeeds.Contains(id))
                ChainFeeds.Add(id);

            return this;
        }

        public ChainPromptRequest WithDataBoost(string? feedMsg, List<string> sources)
        {
            var key = feedMsg.Trim();

            if (string.IsNullOrEmpty(key))
                key = _randomBoostTag;

            //You can repeat an 'instruction' (feedMsg) with different sources too like
            // 'Analyze the stock data and do X, [stock data list from API-A]'
            // 'Analyze the stock data and do X, [stock data list from API-B]'
            Boosters.Add(new KeyValuePair<string, List<string>>(key, sources));

            return this;
        }

        public string BoostersFeedText()
        {
            if (Boosters.Count == 0) return string.Empty;
            
            var sb = new StringBuilder()
                .Append(_boostersSectionTag);

            Boosters.ForEach(kvp =>
            {
                if (kvp.Key != _randomBoostTag)
                {
                    sb.AppendLine()
                      .AppendLine(kvp.Key);
                }

                kvp.Value.ForEach(source => 
                    sb.AppendLine()
                      .AppendLine(source)
                );
            });

            return sb.ToString().Trim();
        }
    }
}
