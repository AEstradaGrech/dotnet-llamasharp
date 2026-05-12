using DotnetLlamaSharp.LangSearch.Models;
using DotnetLlamaSharp.LangSearch.Models.Exceptions;
using DotnetLlamaSharp.LangSearch.Models.Request;
using DotnetLlamaSharp.LangSearch.Models.Response;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DotnetLlamaSharp.LangSearch
{
    public class LangSearchClient : HttpClient, ILangSearchClient
    {
        private readonly LangSearchSettings _settings;
        public LangSearchClient(IOptions<LangSearchSettings> settings) : base() { _settings = settings.Value ?? throw new ArgumentNullException(nameof(LangSearchSettings)); }
        public LangSearchClient(LangSearchSettings settings) : base() { _settings = settings; }
        public async Task<LangSearchWebResponse> GetWebSearchResponse(WebSearchRequest request)
        {
            var response = await this.PostAsJsonAsync<WebSearchRequest>($"{_settings.Domain}/{_settings.WebSearchEndpoint}", request);

            if(!response.IsSuccessStatusCode)
                throw new LangSearchClientException($"{nameof(LangSearchClient)} >> {nameof(GetWebSearchResponse)} >> an error has occured while requesting the data");

            var data = await response.Content.ReadFromJsonAsync<LangSearchWebResponse>();

            if (data.Code != HttpStatusCode.OK)
                throw new LangSearchClientException($"{nameof(LangSearchClient)} >> {data.ErrorMessage}");

            return data;
        }
    }
}
