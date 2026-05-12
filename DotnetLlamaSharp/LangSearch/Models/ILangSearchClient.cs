using DotnetLlamaSharp.LangSearch.Models.Request;
using DotnetLlamaSharp.LangSearch.Models.Response;

namespace DotnetLlamaSharp.LangSearch.Models
{
    public interface ILangSearchClient
    {
        Task<LangSearchWebResponse> GetWebSearchResponse(WebSearchRequest request);
    }
}
