using DotnetLlamaSharp.LangSearch.Models.Enums;

namespace DotnetLlamaSharp.LangSearch.Models
{
    public class LangSearchSettings
    {
        public string Domain { get; set; }
        public string WebSearchEndpoint { get; set; }

        public string RankedSearchEndpoint { get; set; }

        public string ApiKey { get; set; }

        public string UrlFor(ELangEndpoint endpoint) 
            => endpoint switch {
                ELangEndpoint.SEARCH => $"{Domain}/{WebSearchEndpoint}",
                ELangEndpoint.RANKED => $"{Domain}/{RankedSearchEndpoint}",
                _ => ""
            };

    }
}
