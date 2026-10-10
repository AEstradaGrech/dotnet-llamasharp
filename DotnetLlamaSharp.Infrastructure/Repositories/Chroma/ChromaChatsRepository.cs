using Dotnet.Chroma.Repositories;
using Dotnet.Chroma.Repositories.Interfaces;
using Dotnet.Chroma.Repositories.Models;
using Dotnet.Chroma.Repositories.Models.Settings;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{
    public class ChromaChatsRepository : ChromaRepository<ChromaChatsCollection, ChromaChatChunk>, IChromaChatsRepository
    {
        public ChromaChatsRepository(ILogger<ChromaChatsRepository> logger, IOptions<ChromaSettings> dbSettings, IChromaDbClient dbClient) : base(dbClient, dbSettings) { }

        public Task<List<ChromaChatChunk>> GetCollectionSessions(string collectionName)
        {
            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<string>> GetDbCollections()
        {
            var collections = await CollectionsOf((int)EChunkType.CHAT);

            return collections.Count > 0 ? collections.Select(c => c.Name) : Enumerable.Empty<string>();
        }

        public async Task<ChromaChatChunk> GetCurrentSessionChunk(string collectionName)
        {
            var collection = await GetCollection(collectionName);

            return await GetChunkById(collection.Name, collection.GetMeta<ChatCollectionMetadata>().CURRENT_SESSION_ID);
        }

        public async Task<List<ChromaChatChunk>> GetSessionChunks(string collectionName, string sessionId, bool isDescendingOrder = false, int? results = null)
        {
            var collection = await GetCollection(collectionName);

            if (!collection.GetMeta<ChatCollectionMetadata>().SESSION_IDS.Contains(sessionId))
                throw new InvalidOperationException($"{collectionName} >> {nameof(GetSessionChunks)} >> Session {sessionId} does is not part of the collection sessions");

            var sessionChunk = await GetChunkById(collectionName, sessionId);

            //var sessionChunks = await QueryCollection(collectionName, sessionChunk.Embedding,)

            return null;
        }

        public async Task<ChromaChatsCollection> InitCollection(string agentName, string userName, string? embeddingModel = null, int? dimensions = null, string? description = null)
        {
            var validatedName = $"{getValidConstructorNameTag(agentName, isUser: false)}-{getValidConstructorNameTag(userName, isUser: true)}";

            ChromaChunk collectionChunk = DefaultChunk(embeddingModel ?? _settings.EmbeddingModel, dimensions ?? _settings.EmbeddingDimensions);

            collectionChunk.AddMetadata(nameof(ChatCollectionMetadata.AGENT_NAME).ToLower(), agentName);
            collectionChunk.AddMetadata(nameof(ChatCollectionMetadata.USER_NAME).ToLower(), userName);
            collectionChunk.AddMetadata(nameof(ChatCollectionMetadata.CHUNK_TYPE).ToLower(), EChunkType.CHAT);
            collectionChunk.AddMetadata(nameof(ChatCollectionMetadata.DOCUMENT_NAME).ToLower(), validatedName, resetDefault: true);
            collectionChunk.Text = description ?? $"Ollama Rag Chat >> {agentName} - {userName} >> {DateTime.Now}";

            return await CreateCollection(validatedName, collectionChunk);
        }

        public async Task<List<ChromaQueryChunk>> QueryCollection(string collection, ReadOnlyMemory<float> queryEmbedding, int resultsNumber, bool withSessions = false, Dictionary<string, object> filters = null)
        {
            if (!withSessions && !filters.ContainsKey(nameof(ChatChunkMetadata).ToLower()))
                filters.Add(nameof(ChatChunkMetadata.CHAT_INIT).ToLower(), false);

            return await QueryCollection(collection, queryEmbedding, resultsNumber, filters);
        }

        private string getValidConstructorNameTag(string nameTag, bool isUser)
        {
            nameTag = nameTag.Trim().Replace(" ", "");

            if (string.IsNullOrWhiteSpace(nameTag))
                throw new InvalidDataException($"Bad Collection name tag formation. {(isUser ? "USER" : "AGENT")} NAME part is empty");

            return nameTag;
        }
    }
}
