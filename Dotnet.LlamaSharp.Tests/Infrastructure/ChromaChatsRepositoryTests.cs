using Dotnet.Chroma.Repositories;
using Dotnet.Chroma.Repositories.Interfaces;
using Dotnet.Chroma.Repositories.Models;
using Dotnet.Chroma.Repositories.Models.Client.Response;
using Dotnet.Chroma.Repositories.Models.Settings;
using DotnetLlamaSharp.Infrastructure.Repositories.Chroma;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Dotnet.LlamaSharp.Tests.Infrastructure
{
    // ChromaChatsRepository builds on top of ChromaRepository<TCol, TChunk>, which talks to Chroma
    // exclusively through IChromaDbClient. Mocking that interface directly (with Moq) is enough to
    // exercise the repository logic - no HTTP layer / WireMock is involved anymore.

    public class ChromaChatsRepositoryTests
    {
        private readonly Mock<ILogger<ChromaChatsRepository>> _mockLogger;
        private readonly Mock<IChromaDbClient> _mockClient;
        private readonly IOptions<ChromaSettings> _settings;

        public ChromaChatsRepositoryTests()
        {
            _mockLogger = new Mock<ILogger<ChromaChatsRepository>>();
            _mockClient = new Mock<IChromaDbClient>();
            _settings = Options.Create(new ChromaSettings());
        }

        private ChromaChatsRepository CreateSut()
            => new ChromaChatsRepository(_mockLogger.Object, _settings, _mockClient.Object);

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
        // El override del base QueryCollection(6-param) intercepta los filtros antes de la llamada real.

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
            StubChunkGet("col-uuid", chunkId: "session1", isSessionChunk: true);
            var sut = CreateSut();

            // Act
            var result = await sut.GetCurrentSessionChunk("test-col");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("session1");
        }

        [Fact]
        public async Task GetCurrentSessionChunk_CollectionNotFound_ThrowsException()
        {
            // Arrange - IChromaDbClient.GetCollection returns null -> ChromaRepository.GetCollection returns null
            _mockClient
                .Setup(c => c.GetCollection("nonexistent-col"))
                .ReturnsAsync((ChromaCollection)null!);
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
            // Arrange - implementation is currently incomplete: returns null after validation
            SetupCollectionMock("test-col", "col-uuid", sessionIds: "session1", currentSessionId: "session1");
            StubChunkGet("col-uuid", chunkId: "session1", isSessionChunk: true);
            var sut = CreateSut();

            // Act
            var result = await sut.GetSessionChunks("test-col", "session1");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetSessionChunks_SessionNotInCollection_ThrowsInvalidOperationException()
        {
            // Arrange - SESSION_IDS = "session1", requested session = "session99" -> not found
            SetupCollectionMock("test-col", "col-uuid", sessionIds: "session1", currentSessionId: "session1");
            var sut = CreateSut();

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

        // --- Mock helpers ---

        // Sets up IChromaDbClient so that ChromaRepository.GetCollection("collectionName") succeeds,
        // returning the collection chunk (ID "0") shaped as a chat collection.
        private void SetupCollectionMock(string collectionName, string collectionId, string sessionIds, string currentSessionId)
        {
            _mockClient
                .Setup(c => c.ListCollections())
                .ReturnsAsync(new List<string> { collectionName });

            _mockClient
                .Setup(c => c.GetCollection(collectionName))
                .ReturnsAsync(new ChromaCollection { Id = collectionId, Name = collectionName });

            _mockClient
                .Setup(c => c.GetDocuments(collectionId, It.Is<List<string>>(ids => ids.Count == 1 && ids[0] == "0"), true, null))
                .ReturnsAsync(new ChromaDocumentModel
                {
                    Ids = new List<string> { "0" },
                    Documents = new List<string> { "" },
                    Embeddings = new List<float[]>(),
                    Metadatas = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            ["document_name"] = collectionName,
                            ["chunk_type"] = 3,
                            ["current_session_id"] = currentSessionId,
                            ["session_ids"] = sessionIds,
                            ["total_sessions"] = 1,
                            ["current_session_chunks"] = 0,
                            ["agent_name"] = "TestAgent",
                            ["user_name"] = "TestUser"
                        }
                    }
                });
        }

        // Stubs GetDocuments for a specific chunk id, as retrieved via GetChunkById.
        private void StubChunkGet(string collectionId, string chunkId, bool isSessionChunk)
        {
            _mockClient
                .Setup(c => c.GetDocuments(collectionId, It.Is<List<string>>(ids => ids.Count == 1 && ids[0] == chunkId), true, null))
                .ReturnsAsync(new ChromaDocumentModel
                {
                    Ids = new List<string> { chunkId },
                    Documents = new List<string> { "" },
                    Embeddings = new List<float[]>(),
                    Metadatas = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            ["document_name"] = "test-col",
                            ["chunk_type"] = 3,
                            ["chat_init"] = isSessionChunk,
                            ["current"] = isSessionChunk,
                            ["total_messages"] = 0,
                            ["session_id"] = chunkId,
                            ["session_chunks"] = 0
                        }
                    }
                });
        }

        // --- Subclase testable: intercepta la llamada al base QueryCollection(6-param) ---
        // QueryCollection(string, embedding, int, dict, int?, int?) es el único método virtual+no-final en la clase base.

        private class TestableChromaChatsRepository : ChromaChatsRepository
        {
            public Dictionary<string, object>? CapturedFilters { get; private set; }

            public TestableChromaChatsRepository(
                ILogger<ChromaChatsRepository> logger,
                IOptions<ChromaSettings> settings,
                IChromaDbClient client)
                : base(logger, settings, client) { }

            public override Task<List<ChromaQueryChunk>> QueryCollection(
                string collectionName,
                ReadOnlyMemory<float> queryEmbedding,
                int resultsNumber,
                Dictionary<string, object> filters = null!,
                int? offset = null,
                int? limit = null)
            {
                CapturedFilters = filters != null
                    ? new Dictionary<string, object>(filters)
                    : null;

                return Task.FromResult(new List<ChromaQueryChunk>());
            }
        }
    }
}
