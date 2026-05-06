using DocumentFormat.OpenXml.Office2016.Excel;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Infrastructure.Exceptions;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Chroma;


namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{
    public class ChromaChatsRepository : ChromaRepository<ChromaChatsCollection, ChromaChatChunk>, IChromaChatsRepository
    {
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public ChromaChatsRepository(ILogger<ChromaChatsRepository> logger, IOptions<ApiSettings> dbSettings, IChromaClient client) : base(logger, dbSettings, client)
        {
        }

        public Task<List<ChromaChatChunk>> GetCollectionSessions(string collectionName)
        {
            throw new NotImplementedException();
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

        public async Task<ChromaChatsCollection> InitCollection(string agentName, string userName, string? embeddingModel, int? dimensions, string? description = null)
        {
            var validatedName = $"{getValidConstructorNameTag(agentName, isUser: false)}-{getValidConstructorNameTag(userName, isUser: true)}";

            ChromaChunk collectionChunk = DefaultChunk(embeddingModel ?? _settings.DefaultEmbedder, dimensions ?? _settings.DefaultDimensions);

            collectionChunk.AddMetadata(nameof(ChatCollectionMetadata.AGENT_NAME).ToLower(), agentName);
            collectionChunk.AddMetadata(nameof(ChatCollectionMetadata.USER_NAME).ToLower(), userName);
            collectionChunk.AddMetadata(nameof(ChatCollectionMetadata.DOCUMENT_NAME).ToLower(), validatedName, resetDefault: true);
            collectionChunk.Text = description ?? $"Ollama Rag Chat >> {agentName} - {userName} >> {DateTime.Now}";

            return await CreateCollection(validatedName, collectionChunk);
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
