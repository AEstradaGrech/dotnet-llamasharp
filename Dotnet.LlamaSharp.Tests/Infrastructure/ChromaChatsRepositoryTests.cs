using Dotnet.Chroma.Repositories.Models;
using DotnetLlamaSharp.Infrastructure.Repositories.Chroma;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Chroma;
using Moq;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

#pragma warning disable SKEXP0020

namespace Dotnet.LlamaSharp.Tests.Infrastructure
{
    // GetCollection y GetChunkById en ChromaRepository usan ChromaClientExtensions.GetDocuments,
    // que hace llamadas HTTP directas a {ChromaSettings.ServerUrl}/api/v1/collections/{id}/get.
    // Los tests que necesitan estos métodos usan WireMock.Net para interceptar esas llamadas.
    // ListCollectionsAsync y GetCollectionAsync son métodos de IChromaClient y se mockean con Moq.

    public class ChromaChatsRepositoryTests : IDisposable
    {
        private readonly Mock<ILogger<ChromaChatsRepository>> _mockLogger;
        private readonly Mock<IChromaClient> _mockClient;
        private readonly IOptions<ChromaSettings> _settings;
        private WireMockServer? _wireMock;

        public ChromaChatsRepositoryTests()
        {
            _mockLogger = new Mock<ILogger<ChromaChatsRepository>>();
            _mockClient = new Mock<IChromaClient>();
            _settings = Options.Create(new ChromaSettings());
        }

        public void Dispose()
        {
            _wireMock?.Stop();
            _wireMock?.Dispose();
            _wireMock = null;
        }

        private ChromaChatsRepository CreateSut()
            => new ChromaChatsRepository(_mockLogger.Object, _settings, _mockClient.Object);

        private ChromaChatsRepository CreateSutWithWireMock()
            => new ChromaChatsRepository(
                _mockLogger.Object,
                Options.Create(new ChromaSettings { ServerUrl = _wireMock!.Url! }),
                _mockClient.Object);

        private TestableChromaChatsRepository CreateTestableSut()
            => new TestableChromaChatsRepository(_mockLogger.Object, _settings, _mockClient.Object);

        // --- InitCollection: validación de nombre ---

        [Fact]
        public async Task InitCollection_EmptyAgentName_ThrowsInvalidDataException()
        {
            // Arrange
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.InitCollection("", "TestUser");

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*AGENT*");
        }

        [Fact]
        public async Task InitCollection_WhitespaceOnlyAgentName_ThrowsInvalidDataException()
        {
            // Arrange
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.InitCollection("   ", "TestUser");

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*AGENT*");
        }

        [Fact]
        public async Task InitCollection_EmptyUserName_ThrowsInvalidDataException()
        {
            // Arrange
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.InitCollection("TestAgent", "");

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*USER*");
        }

        // --- QueryCollection: mutación de filtros ---
        // El override del base QueryCollection(4-param) intercepta los filtros antes de la llamada real.

        [Fact]
        public async Task QueryCollection_WithoutSessionsAndEmptyFilters_AddsChatInitFalseFilter()
        {
            // Arrange
            var sut = CreateTestableSut();
            var filters = new Dictionary<string, object>();

            // Act
            await sut.QueryCollection("test-col", default, 5, withSessions: false, filters);

            // Assert
            sut.CapturedFilters.Should().ContainKey("chat_init");
            sut.CapturedFilters!["chat_init"].Should().Be((object)false);
        }

        [Fact]
        public async Task QueryCollection_WithSessions_DoesNotMutateFilters()
        {
            // Arrange
            var sut = CreateTestableSut();
            var filters = new Dictionary<string, object>();

            // Act
            await sut.QueryCollection("test-col", default, 5, withSessions: true, filters);

            // Assert
            sut.CapturedFilters.Should().NotContainKey("chat_init");
        }

        // --- GetCurrentSessionChunk ---

        [Fact]
        public async Task GetCurrentSessionChunk_ValidCollection_ReturnsCurrentSessionChunk()
        {
            // Arrange
            SetupCollectionMock("test-col", "col-uuid", sessionIds: "session1", currentSessionId: "session1");
            StubChunkHttpGet("col-uuid", chunkId: "session1", isSessionChunk: true);
            var sut = CreateSutWithWireMock();

            // Act
            var result = await sut.GetCurrentSessionChunk("test-col");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("session1");
        }

        [Fact]
        public async Task GetCurrentSessionChunk_CollectionNotFound_ThrowsException()
        {
            // Arrange – ListCollectionsAsync returns empty → CollectionExists = false → exception
            _mockClient
                .Setup(c => c.ListCollectionsAsync(It.IsAny<CancellationToken>()))
                .Returns(AsyncEnumerable());
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.GetCurrentSessionChunk("nonexistent-col");

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        // --- GetSessionChunks ---

        [Fact]
        public async Task GetSessionChunks_ValidSessionId_ReturnsNull()
        {
            // Arrange – implementation is currently incomplete: returns null after validation
            SetupCollectionMock("test-col", "col-uuid", sessionIds: "session1", currentSessionId: "session1");
            StubChunkHttpGet("col-uuid", chunkId: "session1", isSessionChunk: true);
            var sut = CreateSutWithWireMock();

            // Act
            var result = await sut.GetSessionChunks("test-col", "session1");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetSessionChunks_SessionNotInCollection_ThrowsInvalidOperationException()
        {
            // Arrange – SESSION_IDS = "session1", requested session = "session99" → not found
            SetupCollectionMock("test-col", "col-uuid", sessionIds: "session1", currentSessionId: "session1");
            var sut = CreateSutWithWireMock();

            // Act
            Func<Task> act = () => sut.GetSessionChunks("test-col", "session99");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*session99*");
        }

        // --- GetCollectionSessions ---

        [Fact]
        public async Task GetCollectionSessions_ThrowsNotImplementedException()
        {
            // Arrange
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.GetCollectionSessions("any-collection");

            // Assert
            await act.Should().ThrowAsync<NotImplementedException>();
        }

        // --- Mock / stub helpers ---

        // Sets up IChromaClient mocks and WireMock HTTP stub for the collection metadata chunk (ID "0").
        // requestDocuments calls ChromaClientExtensions.GetDocuments which POSTs to:
        //   {ServerUrl}/api/v1/collections/{collectionId}/get
        private void SetupCollectionMock(string collectionName, string collectionId,
            string sessionIds, string currentSessionId)
        {
            _wireMock = WireMockServer.Start();

            // IChromaClient mocks (used by CollectionExists and GetChunkById → GetCollectionAsync)
            _mockClient
                .Setup(c => c.ListCollectionsAsync(It.IsAny<CancellationToken>()))
                .Returns(AsyncEnumerable(collectionName));

            _mockClient
                .Setup(c => c.GetCollectionAsync(collectionName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChromaCollectionModel { Id = collectionId, Name = collectionName });

            // HTTP stub: collection metadata chunk (ID "0")
            var collectionMetaJson = JsonSerializer.Serialize(new
            {
                ids = new[] { "0" },
                embeddings = (object?)null,
                documents = new[] { "" },
                metadatas = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["document_name"]          = collectionName,
                        ["chunk_type"]             = 3,
                        ["current_session_id"]     = currentSessionId,
                        ["session_ids"]            = sessionIds,
                        ["total_sessions"]         = 1,
                        ["current_session_chunks"] = 0,
                        ["agent_name"]             = "TestAgent",
                        ["user_name"]              = "TestUser"
                    }
                }
            });

            _wireMock
                .Given(Request.Create()
                    .WithPath($"/api/v1/collections/{collectionId}/get")
                    .UsingPost()
                    .WithBody(body => body != null && (body.Contains("\"0\"") || body.Contains("'0'"))))
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(collectionMetaJson));
        }

        // HTTP stub for a specific chunk retrieved via GetChunkById.
        private void StubChunkHttpGet(string collectionId, string chunkId, bool isSessionChunk)
        {
            var chunkJson = JsonSerializer.Serialize(new
            {
                ids = new[] { chunkId },
                embeddings = (object?)null,
                documents = new[] { "" },
                metadatas = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["document_name"]  = "test-col",
                        ["chunk_type"]     = 3,
                        ["chat_init"]      = isSessionChunk,
                        ["current"]        = isSessionChunk,
                        ["total_messages"] = 0,
                        ["session_id"]     = chunkId,
                        ["session_chunks"] = 0
                    }
                }
            });

            _wireMock!
                .Given(Request.Create()
                    .WithPath($"/api/v1/collections/{collectionId}/get")
                    .UsingPost()
                    .WithBody(body => body != null && body.Contains($"\"{chunkId}\"")))
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(chunkJson));
        }

        private static async IAsyncEnumerable<string> AsyncEnumerable(params string[] items)
        {
            foreach (var item in items)
                yield return item;
        }

        // --- Subclase testable: intercepta la llamada al base QueryCollection(4-param) ---
        // QueryCollection(string, embedding, int, dict) es el único método virtual+no-final en la clase base.

        private class TestableChromaChatsRepository : ChromaChatsRepository
        {
            public Dictionary<string, object>? CapturedFilters { get; private set; }

            public TestableChromaChatsRepository(
                ILogger<ChromaChatsRepository> logger,
                IOptions<ChromaSettings> settings,
                IChromaClient client)
                : base(logger, settings, client) { }

            public override Task<List<ChromaQueryChunk>> QueryCollection(
                string name,
                ReadOnlyMemory<float> queryEmbedding,
                int resultsNumber,
                Dictionary<string, object> filters)
            {
                CapturedFilters = filters != null
                    ? new Dictionary<string, object>(filters)
                    : null;

                return Task.FromResult(new List<ChromaQueryChunk>());
            }
        }
    }
}
