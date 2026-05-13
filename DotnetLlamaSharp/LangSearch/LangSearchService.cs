using Dotnet.Chroma.Repositories.Models.Interfaces;
using DotnetLlamaSharp.Domain.Services.Prompting;
using DotnetLlamaSharp.LangSearch.Models;
using DotnetLlamaSharp.LangSearch.Models.Exceptions;
using DotnetLlamaSharp.LangSearch.Models.Request;
using DotnetLlamaSharp.LangSearch.Models.Response;
using DotnetLlamaSharp.LangSearch.Models.Response.RankedSearch;
using DotnetLlamaSharp.LangSearch.Models.Response.WebSearch;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DotnetLlamaSharp.LangSearch
{
    public class LangSearchService : ILangSearchService
    {
        private readonly ILangSearchClient _client;
        private readonly IChromaChunksRepository _dataSource;
        public LangSearchService(ILangSearchClient client, /*DevOnly*/IChromaChunksRepository dataSource)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _dataSource = dataSource;
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

        public async Task<RankedData> GetReRankData(RankedSearchRequest request)
        {
            var dataMock = await _dataSource.GetCollectionPage("rags", null, withEmbeddings: false, pageSize: 10, page: 3);

            request.QueriedDocuments = dataMock.Chunks.Select(chunk => chunk.Text).ToList();

            var data = await _client.GetRankedSearchResponse(request);

            return new RankedData { Model = data.Model, Query = request.Query, Results = data.Results.Select(doc => new RankedDocument { Index = doc.Index, Score = doc.Score, Text = doc.Document.Text }).ToList() };
        }
    }
}
