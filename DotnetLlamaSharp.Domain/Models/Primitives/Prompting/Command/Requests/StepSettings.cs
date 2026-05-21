using OllamaSharp.Models;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    // ChainRequest<TReq> where TReq : CommandPromptRequest
    // TReq CommandReq get;
    // NO HACE FALTA QUE SEA GENERICO es mejor pasar la base (PromptCommandRequest <- el tipo lo comprueba el command y se castea ahi para hacer cosas esto es solo para llevarlo y configurarlo desde Chain y en runtime)
    // PromptCommandRequest CommandRequest { get;} --> .Tal(_factory.GetAsJsoneable<StringChoinceCommand, string>(instruction, settings),
    //                                                      new ChainRequest(commandReq: new StringChoiceRequest { Choices=reqDto.Choices }), feedFwd: "")
    //                                                  Lo confi
    public class StepSettings
    {
        private const string _randomBoostTag = "RNDM_SET";
        private const string _boostersSectionTag = "## ADDITIONAL INFORMATION: "; 
        public StepSettings() 
        {
            ChainFeeds = new List<Guid>();
            Boosters = new List<KeyValuePair<string, List<string>>>();
        }

        public StepSettings(PromptCommandRequest request) : this()
        {
            CommandRequest = request;
        }


        //public StepSettings(string message, string? guidanceMessage, CommandSettings? settings, string? model = null) 
        //    : base(message, guidanceMessage,settings != null ? settings.ToOllamaRequest() : new RequestOptions(), model) 
        //{
        //    ChainFeeds = new List<Guid>();
        //    Boosters = new List<KeyValuePair<string, List<string>>>();
        //}

        public PromptCommandRequest CommandRequest { get; }
        public List<Guid> ChainFeeds { get; set; } // Runner Ids to feed the context from
        
        public List<KeyValuePair<string,List<string>>> Boosters { get; set;  } // Additional sources of data to enhance the prompt, add few-shot examples or whatever This will be appended at the end of the system message as "#ADDITIONAL INFORMATION: <rebujito feed message to guide / interpret the feeds> 

        //public CommandSettings? CommandSettings { get; set; } = null; // ?? _runner.DefaultSettings (from userReq)
        public StepSettings FeedFrom(Guid id)
        {
            if (!ChainFeeds.Contains(id))
                ChainFeeds.Add(id);

            return this;
        }
        //ChainPromptRequest(CommandRequest specificRequest) esto tiene que llegar a forge<TReq> -> JsonPrompt(TRequest req (como 'T' llega a Command
        //                                                  es decir: ChainPrompt comunica info entre steps (guidance message, TReq es la peticion del step al command
        //                                                  B : ChainPromptRequest NO deriva de Command, es solo un recipiente y toda la data que se esta guardando ahi
        //                                                      tiene que ir en TReq --> ChainPrompt es un recipiente, un orquestador y/o un puente de informacion entre
        //                                                      - ChainConfig (info guardada en los .Tal().Pascual()
        //                                                      - Runner (lleva la request , TReq y el command ademas del ChainRunner (nexo)
        //                                                      - RuntimeSupporters (pasan info de uno a otro guardandola en su request -> req.Guidance = prev.Data y eso se pasa al command, OnReport.FeededMsg = req.Guidance
        //                                                      - RunnerCommand
        //                                                      
        // TRequest GetCommandRequest<TRequest>() -->
        public StepSettings WithDataBoost(string? feedMsg, List<string> sources)
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
