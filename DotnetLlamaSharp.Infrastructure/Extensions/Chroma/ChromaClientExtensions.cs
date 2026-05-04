using DotnetLlamaSharp.Infrastructure.Models;
using Microsoft.SemanticKernel.Connectors.Chroma;
using System.Net.Http.Json;
using System.Text.Json;

namespace DotnetLlamaSharp.Infrastructure.Extensions.Chroma
{
    public static class ChromaClientExtensions
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /*
         Chroma supports a subset of operators, mainly:
            Logical
            { "$and": [ ... ] }
            { "$or": [ ... ] }
            Comparison
            { "field": "value" }
            { "field": { "$eq": "value" } }
            { "field": { "$gt": 5 } }
            { "field": { "$lt": 10 } }

            That’s basically it.
         */
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public static async Task<ChromaQueryResultModel> FilterQuery(this IChromaClient chromaClient, string baseUrl, string collectionId, int resultsNumber, List<ReadOnlyMemory<float>> queryEmbeddings, Dictionary<string, object> filters)
        {
            if (filters.Any() && filters.Count > 1)
            {
                var conditions = new List<object>();
                filters.Keys.ToList().ForEach(key => conditions.Add(new Dictionary<string, object> { [key] = filters[key] }));
                filters = new Dictionary<string, object>();
                filters.Add("$and", conditions);
            }
            
            var request = new ChromaClientQueryRequest
            {
                Embeddings = queryEmbeddings,
                Results = resultsNumber,
                MetadataFilters = filters
            };
            
            var result = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/v1/collections/{collectionId}/query", request);

            result.EnsureSuccessStatusCode();
            
            return await result.Content.ReadFromJsonAsync<ChromaQueryResultModel>();
        }
    }
}
