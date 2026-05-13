using DotnetLlamaSharp.LangSearch.Models;
using DotnetLlamaSharp.LangSearch.Models.Enums;
using DotnetLlamaSharp.LangSearch.Models.Exceptions;
using DotnetLlamaSharp.LangSearch.Models.Request;
using DotnetLlamaSharp.LangSearch.Models.Response;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace DotnetLlamaSharp.LangSearch
{
    public class LangSearchClient : ILangSearchClient
    {
        private readonly LangSearchSettings _settings;
        private readonly HttpClient _httpClient;
        public LangSearchClient(HttpClient client, IOptions<LangSearchSettings> settings) : base() 
        { 
            _httpClient = client;
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(LangSearchSettings));
        }

        public async Task<LangSearchWebResponse> GetWebSearchResponse(WebSearchRequest request)
        {
            try
            {
                addRequestHeaders();

                var x = _httpClient.BaseAddress;
                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                
                options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                
                var response = await _httpClient.PostAsJsonAsync<WebSearchRequest>(_settings.UrlFor(ELangEndpoint.SEARCH), request, options);

                if (!response.IsSuccessStatusCode)
                    throw new LangSearchClientException($"{nameof(LangSearchClient)} >> {nameof(GetWebSearchResponse)} >> an error has occured while requesting the data");

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

                var response = await _httpClient.PostAsJsonAsync<RankedSearchRequest>(_settings.UrlFor(ELangEndpoint.RANKED), request);

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
            _httpClient.DefaultRequestHeaders.Add("Accept", ["application/json"]);
            _httpClient.DefaultRequestHeaders.Add("Authorization", [$"Bearer {_settings.ApiKey}"]);
        }
    }
}
