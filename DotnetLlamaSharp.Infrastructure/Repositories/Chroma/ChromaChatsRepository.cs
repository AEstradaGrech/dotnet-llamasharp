using DocumentFormat.OpenXml.Office2016.Excel;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Chroma;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Infrastructure.Repositories.Chroma
{
    public class ChromaChatsRepository : ChromaRepository<ChromaChatCollection, ChromaChatChunk>, IChromaChatsRepository
    {
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public ChromaChatsRepository(ILogger<ChromaChatsRepository> logger, IOptions<ApiSettings> dbSettings, IChromaClient client) : base(logger, dbSettings, client)
        {
        }

        public async Task<ChromaChatChunk> GetCurrentSessionChunk(string collectionName)
        {
            var collection = await GetCollection(collectionName);

            return await GetChunkById(collection.Name, collection.GetMeta<ChatCollectionMetadata>().CURRENT_SESSION_ID);
        }

        public async Task<ChromaChatCollection> InitCollection(string agentName, string userName, string? embeddingModel, int? dimensions, string? description = null)
        {
            var validatedName = $"{getValidConstructorNameTag(agentName, isUser: false)}-{getValidConstructorNameTag(userName, isUser: true)}";

            ChromaChunk collectionChunk = await DefaultChunk(embeddingModel ?? _settings.DefaultEmbedder, dimensions ?? _settings.DefaultDimensions);

            collectionChunk.AddMetadata(nameof(ChatCollectionMetadata.AGENT_NAME).ToLower(), agentName);
            collectionChunk.AddMetadata(nameof(ChatCollectionMetadata.USER_NAME).ToLower(), userName);
            collectionChunk.AddMetadata(nameof(ChatCollectionMetadata.DOCUMENT).ToLower(), validatedName, resetDefault: true);
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
