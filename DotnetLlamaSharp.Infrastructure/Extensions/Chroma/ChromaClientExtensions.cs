using DotnetLlamaSharp.Infrastructure.Models;
using Microsoft.SemanticKernel.Connectors.Chroma;
using System.Net.Http.Json;
using System.Text.Json;

namespace DotnetLlamaSharp.Infrastructure.Extensions.Chroma
{
    public static class ChromaClientExtensions
    {
        private static readonly HttpClient _httpClient = new HttpClient();
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public static async Task<ChromaQueryResultModel> FilterQuery(this IChromaClient chromaClient, string baseUrl, string collectionId, int resultsNumber, List<ReadOnlyMemory<float>> queryEmbeddings, Dictionary<string, object> filters)
        {
            
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
