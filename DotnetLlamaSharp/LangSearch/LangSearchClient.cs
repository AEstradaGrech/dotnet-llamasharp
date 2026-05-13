using DotnetLlamaSharp.LangSearch.Models;
using DotnetLlamaSharp.LangSearch.Models.Exceptions;
using DotnetLlamaSharp.LangSearch.Models.Request;
using DotnetLlamaSharp.LangSearch.Models.Response;
using Microsoft.Extensions.Options;
using System.Net;


namespace DotnetLlamaSharp.LangSearch
{
    public class LangSearchClient : HttpClient, ILangSearchClient
    {
        private readonly LangSearchSettings _settings;
        public LangSearchClient(IOptions<LangSearchSettings> settings) : base() { _settings = settings.Value ?? throw new ArgumentNullException(nameof(LangSearchSettings)); }
        public LangSearchClient(LangSearchSettings settings) : base() { _settings = settings; }

        public async Task<LangSearchWebResponse> GetWebSearchResponse(WebSearchRequest request)
        {
            try
            {
                addRequestHeaders();

                var response = await this.PostAsJsonAsync<WebSearchRequest>($"{_settings.Domain}/{_settings.WebSearchEndpoint}", request);

                if (!response.IsSuccessStatusCode)
                    throw new LangSearchClientException($"{nameof(LangSearchClient)} >> {nameof(GetWebSearchResponse)} >> an error has occured while requesting the data");

                var json = await response.Content.ReadAsStringAsync();
                var data = await response.Content.ReadFromJsonAsync<LangSearchWebResponse>();

                if (data.Code != HttpStatusCode.OK)
                    throw new LangSearchClientException($"{nameof(LangSearchClient)} >> {data.ErrorMessage}");

                return data;
            }
            catch(Exception ex)
            {
                if (ex.GetType() != typeof(LangSearchClientException))
                    throw new LangSearchClientException($"{nameof(LangSearchClient)} >> An error has occured while making the request to endpoint: {_settings.WebSearchEndpoint} >> {ex.Message}");

                throw ex;
            }
        }

        public async Task<LangSearchRankedResponse> GetRankedSearchResponse(RankedSearchRequest request)
        {
            try
            {
                addRequestHeaders();

                var response = await this.PostAsJsonAsync<RankedSearchRequest>($"{_settings.Domain}/{_settings.RankedSearchEndpoint}", request);

                if (!response.IsSuccessStatusCode)
                    throw new LangSearchClientException($"{nameof(LangSearchClient)} >> {nameof(GetRankedSearchResponse)} >> an error has occured while requesting the data");

                var json = await response.Content.ReadAsStringAsync();
                var data = await response.Content.ReadFromJsonAsync<LangSearchRankedResponse>();

                if (data.Code != HttpStatusCode.OK)
                    throw new LangSearchClientException($"{nameof(LangSearchClient)} >> {data.ErrorMessage}");

                return data;
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(LangSearchClientException))
                    throw new LangSearchClientException($"{nameof(LangSearchClient)} >> An error has occured while making the request to endpoint: {_settings.RankedSearchEndpoint} >> {ex.Message}");

                throw ex;
            }
        }

        private void addRequestHeaders()
        {
            DefaultRequestHeaders.Add("Accept", ["application/json"]);
            DefaultRequestHeaders.Add("Authorization", [$"Bearer {_settings.ApiKey}"]);
        }
    }
}
