using DotnetLlamaSharp.Domain.Services.Prompting;
using DotnetLlamaSharp.LangSearch.Models;
using DotnetLlamaSharp.LangSearch.Models.Exceptions;
using DotnetLlamaSharp.LangSearch.Models.Request;
using DotnetLlamaSharp.LangSearch.Models.Response;
using DotnetLlamaSharp.LangSearch.Models.Response.WebSearch;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DotnetLlamaSharp.LangSearch
{
    public class LangSearchService : ILangSearchService
    {
        private readonly ILangSearchClient _client;
        public LangSearchService(ILangSearchClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<WebPage> GetWebPage(WebSearchRequest request)
        {
            var data = await GetWebSearchData(request);

            return data.WebPages;
        }

        public async Task<SearchData> GetWebSearchData(WebSearchRequest request)
        {
            var response = await _client.GetWebSearchResponse(request);

            return response.Data;
        }
    }
}
