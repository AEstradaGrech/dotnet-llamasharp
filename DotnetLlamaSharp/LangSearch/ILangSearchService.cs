using DotnetLlamaSharp.LangSearch.Models.Request;
using DotnetLlamaSharp.LangSearch.Models.Response.RankedSearch;
using DotnetLlamaSharp.LangSearch.Models.Response.WebSearch;

namespace DotnetLlamaSharp.LangSearch
{
    public interface ILangSearchService
    {
        Task<SearchData> GetWebSearchData(WebSearchRequest request);
        Task<WebPage> GetWebPage(WebSearchRequest request);

        Task<RankedData> GetReRankData(RankedSearchRequest request);

        //TODO: SearchAndRank with LangSearch
    }
}
