using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DotnetLlamaSharp.Infrastructure.Extensions.Chroma.Models;

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

        public static async Task<DocumentGetResultModel> UpsertDocument(this IChromaClient chromaClient, string baseUrl, string collectionId, ChromaClientUpsertRequest request, bool isCreate = false)
        {
            var result = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/v1/collections/{collectionId}/{(isCreate ? "add" : "update")}", request);

            result.EnsureSuccessStatusCode();

            return await chromaClient.GetDocuments(baseUrl, collectionId, request.Ids, withEmbeddings: request.Embeddings != null && request.Embeddings.Count > 0);
        }

        public static async Task<DocumentGetResultModel> UpsertDocument(this IChromaClient chromaClient, string baseUrl, string collectionId, string id, string text, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata, bool isCreate = false)
            => await chromaClient.UpsertDocument(baseUrl, collectionId, 
                new ChromaClientUpsertRequest {
                    Ids = [id],
                    Documents = [text],
                    Embeddings = [embedding],
                    Metadatas = [metadata]
                }, isCreate: isCreate);
        
        public static async Task<DocumentGetResultModel> UpsertDocuments(this IChromaClient chromaClient, string baseUrl, string collectionId, List<string> ids, List<string>? texts, List<ReadOnlyMemory<float>>? embeddings, List<Dictionary<string, object>>? metadatas, bool isCreate = false)
            => await chromaClient.UpsertDocument(baseUrl, collectionId,
                new ChromaClientUpsertRequest {
                    Ids = ids,
                    Documents = texts,
                    Embeddings = embeddings,
                    Metadatas = metadatas
                }, isCreate: isCreate);

        public static async Task<DocumentGetResultModel> GetDocuments(this IChromaClient chromaClient, string baseUrl, string collectionId, List<string> ids, bool withEmbeddings = true)
            => await chromaClient.GetDocuments(baseUrl, collectionId, withEmbeddings, filters: null, pageSize: null, skip: null, ids);
        
        public static async Task<DocumentGetResultModel> GetDocuments(this IChromaClient chromaClient, string baseUrl, string collectionId, ChromaClientGetRequest request)
        {
            var result = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/v1/collections/{collectionId}/get", request);

            result.EnsureSuccessStatusCode();

            return await result.Content.ReadFromJsonAsync<DocumentGetResultModel>();
        }

        public static async Task<DocumentGetResultModel> GetDocuments(this IChromaClient chromaClient, string baseUrl, string collectionId, bool withEmbeddings, Dictionary<string, object>? filters = null, int? pageSize = null, int? skip = null, List<string>? ids = null)
        {
            if(filters != null)
            {
                if (filters.Any() && filters.Count > 1)
                {
                    var conditions = new List<object>();
                    filters.Keys.ToList().ForEach(key => conditions.Add(new Dictionary<string, object> { [key] = filters[key] }));
                    filters = new Dictionary<string, object>();
                    filters.Add("$and", conditions);
                }
            }
            
            var includes = new List<string> {"metadatas", "documents" };

            if (withEmbeddings)
                includes.Add("embeddings");

            return await chromaClient.GetDocuments(baseUrl, collectionId, 
                new ChromaClientGetRequest {
                    Ids = ids,
                    Results = pageSize,
                    Skip = skip,
                    MetadataFilters = filters,
                    Includes = includes
                });
        }

        public static async Task<DocumentsQueryResultModel> QueryDocuments(this IChromaClient chromaClient, string baseUrl, string collectionId, List<ReadOnlyMemory<float>> queryEmbeddings, int nResults, Dictionary<string, object>? filters = null)
        {
            if (filters != null)
            {
                if (filters.Any() && filters.Count > 1)
                {
                    var conditions = new List<object>();
                    filters.Keys.ToList().ForEach(key => conditions.Add(new Dictionary<string, object> { [key] = filters[key] }));
                    filters = new Dictionary<string, object>();
                    filters.Add("$and", conditions);
                }
            }

            var request = new ChromaClientQueryRequest
            {
                Embeddings = queryEmbeddings,
                Results = nResults,
                MetadataFilters = filters,
                Includes = ["metadatas", "documents", "embeddings", "distances"]
            };

            var result = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/v1/collections/{collectionId}/query", request);

            result.EnsureSuccessStatusCode();

            return await result.Content.ReadFromJsonAsync<DocumentsQueryResultModel>();
        }
    }
}
