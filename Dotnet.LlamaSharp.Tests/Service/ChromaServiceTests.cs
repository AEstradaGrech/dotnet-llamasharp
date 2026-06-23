using Dotnet.Chroma.Repositories.Models;
using Dotnet.Chroma.Repositories.Models.Interfaces;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Embedding;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Services.Embeddings;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

#pragma warning disable SKEXP0020

namespace Dotnet.LlamaSharp.Tests.Service
{
    public class ChromaServiceTests
    {
        private readonly Mock<ILogger<ChromaService>> _mockLogger;
        private readonly Mock<IChromaFilesService> _mockFileMgmtService;
        private readonly Mock<IEmbeddingsService> _mockEmbeddingsService;
        private readonly Mock<IChromaChunksRepository> _mockRepo;
        private readonly Mock<IChromaSysChunksRepository> _mockSysRepo;
        private readonly Mock<IChromaChatsRepository> _mockChatsRepo;

        public ChromaServiceTests()
        {
            _mockLogger = new Mock<ILogger<ChromaService>>();
            _mockFileMgmtService = new Mock<IChromaFilesService>();
            _mockEmbeddingsService = new Mock<IEmbeddingsService>();
            _mockRepo = new Mock<IChromaChunksRepository>();
            _mockSysRepo = new Mock<IChromaSysChunksRepository>();
            _mockChatsRepo = new Mock<IChromaChatsRepository>();
        }

        private ChromaService CreateSut() => new ChromaService(
            _mockLogger.Object, _mockFileMgmtService.Object, _mockEmbeddingsService.Object,
            _mockRepo.Object, _mockSysRepo.Object, _mockChatsRepo.Object);

        private static ChromaChunksCollection<ChromaChunk> CreateChunksCollection(string name = "test-col") =>
            new("col-id", name, "desc", new Dictionary<string, object>
            {
                ["model"] = "test-embedder", ["dimensions"] = 512,
                ["type"] = 1, ["document_name"] = name
            });

        private static SysChunksCollection CreateSysCollection(string name = "sys-col") =>
            new("col-id", name, "desc", new Dictionary<string, object>
            {
                ["model"] = "test-embedder", ["dimensions"] = 512,
                ["type"] = (int)EChunkType.SYSTEM, ["document_name"] = name,
                ["total_chunks"] = 0, ["chunk_type"] = (int)EChunkType.SYSTEM
            });

        private static ChromaChatsCollection CreateChatsCollection(string name = "chat-col", string sessionId = "sess1") =>
            new("col-id", name, "desc", new Dictionary<string, object>
            {
                ["model"] = "test-embedder", ["dimensions"] = 512,
                ["document_name"] = name, ["chunk_type"] = (int)EChunkType.CHAT,
                ["total_chunks"] = 0, ["total_sessions"] = 1,
                ["session_ids"] = sessionId, ["current_session_id"] = sessionId,
                ["current_session_chunks"] = 0, ["agent_name"] = "agent", ["user_name"] = "user"
            });

        private static ChromaSysChunk CreateSysChunk(string name = "msg-name") =>
            new("chunk-id", "msg text", new ReadOnlyMemory<float>(new[] { 0.1f }), new Dictionary<string, object>
            {
                ["document_name"] = name, ["tag"] = "base", ["version"] = "v1",
                ["model"] = "test-embedder", ["dimensions"] = 512, ["type"] = (int)EChunkType.SYSTEM
            });

        private static ModelEmbeddings CreateModelEmbeddings()
        {
            var emb = new TextEmbedding("text", new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f }), 2);
            return new ModelEmbeddings { Model = "test-embedder", GeneratedEmbeddings = [emb] };
        }

        // --- QueryCollection ---

        [Fact]
        public async Task QueryCollection_ValidQuery_ReturnsChromaQuery()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetCollection("test-col")).ReturnsAsync(CreateChunksCollection());
            _mockEmbeddingsService.Setup(e => e.GenerateEmbeddings(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>()))
                                  .ReturnsAsync(CreateModelEmbeddings());
            _mockRepo.Setup(r => r.QueryCollection("test-col", It.IsAny<ReadOnlyMemory<float>>(), 5, It.IsAny<Dictionary<string, object>>()))
                     .ReturnsAsync(new List<ChromaQueryChunk>());
            var sut = CreateSut();

            // Act
            var result = await sut.QueryCollection("test-col", "my query", 5, new Dictionary<string, object>());

            // Assert
            result.Query.Should().Be("my query");
            result.Collection.Should().Be("test-col");
            result.EmbeddingModel.Should().Be("test-embedder");
            result.Dimensions.Should().Be(512);
        }

        [Fact]
        public async Task QueryCollection_EmptyQuery_ThrowsArgumentNullException()
        {
            // Arrange
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.QueryCollection("test-col", "", 5, new Dictionary<string, object>());

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        // --- AddSysMessage ---

        [Fact]
        public async Task AddSysMessage_NewMessage_InsertsAndReturnsChunk()
        {
            // Arrange
            var input = new ChromaSysChunk { Name = "msg-name", Tag = "base", Text = "Hello" };
            var inserted = CreateSysChunk();

            _mockSysRepo.Setup(r => r.GetCollection("sys-col")).ReturnsAsync(CreateSysCollection());
            _mockSysRepo.Setup(r => r.ExistsMessage("sys-col", "msg-name", "v1")).ReturnsAsync(false);
            _mockSysRepo.Setup(r => r.DefaultChunk("sys-col")).ReturnsAsync(new ChromaSysChunk());
            _mockEmbeddingsService.Setup(e => e.GenerateEmbeddings(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>()))
                                  .ReturnsAsync(CreateModelEmbeddings());
            _mockSysRepo.Setup(r => r.InsertChunk("sys-col", It.IsAny<ChromaSysChunk>(), It.IsAny<Dictionary<string, object>>()))
                        .ReturnsAsync(inserted);
            var sut = CreateSut();

            // Act
            var result = await sut.AddSysMessage("sys-col", input, "v1");

            // Assert
            result.Should().Be(inserted);
            _mockSysRepo.Verify(r => r.InsertChunk("sys-col", It.IsAny<ChromaSysChunk>(), It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task AddSysMessage_DuplicateNameVersion_ThrowsInvalidOperationException()
        {
            // Arrange
            var input = new ChromaSysChunk { Name = "existing-msg", Tag = "base", Text = "Hello" };

            _mockSysRepo.Setup(r => r.GetCollection("sys-col")).ReturnsAsync(CreateSysCollection());
            _mockSysRepo.Setup(r => r.ExistsMessage("sys-col", "existing-msg", "v1")).ReturnsAsync(true);
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.AddSysMessage("sys-col", input, "v1");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*existing-msg*");
        }

        // --- DeleteSysMessageById ---

        [Fact]
        public async Task DeleteSysMessageById_ChunkFound_ReturnsDeletedChunk()
        {
            // Arrange
            var chunk = CreateSysChunk();

            _mockSysRepo.Setup(r => r.GetChunkById("sys-col", "chunk-id")).ReturnsAsync(chunk);
            _mockSysRepo.Setup(r => r.DeleteChunk("sys-col", "chunk-id")).ReturnsAsync(true);
            var sut = CreateSut();

            // Act
            var result = await sut.DeleteSysMessageById("sys-col", "chunk-id");

            // Assert
            result.Should().Be(chunk);
        }

        [Fact]
        public async Task DeleteSysMessageById_DeleteFails_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockSysRepo.Setup(r => r.GetChunkById("sys-col", "chunk-id")).ReturnsAsync(CreateSysChunk());
            _mockSysRepo.Setup(r => r.DeleteChunk("sys-col", "chunk-id")).ReturnsAsync(false);
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.DeleteSysMessageById("sys-col", "chunk-id");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*chunk-id*");
        }

        // --- PatchSysMessage ---

        [Fact]
        public async Task PatchSysMessage_ValidText_ReturnsUpdatedChunk()
        {
            // Arrange
            var updated = CreateSysChunk("updated");

            _mockSysRepo.Setup(r => r.GetChunkById("sys-col", "chunk-id")).ReturnsAsync(CreateSysChunk());
            _mockEmbeddingsService.Setup(e => e.GenerateEmbeddings(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>()))
                                  .ReturnsAsync(CreateModelEmbeddings());
            _mockSysRepo.Setup(r => r.UpsertChunk("sys-col", It.IsAny<ChromaSysChunk>(), It.IsAny<Dictionary<string, object>>()))
                        .ReturnsAsync(updated);
            var sut = CreateSut();

            // Act
            var result = await sut.PatchSysMessage("sys-col", "chunk-id", "new text");

            // Assert
            result.Should().Be(updated);
        }

        [Fact]
        public async Task PatchSysMessage_EmptyText_ThrowsBadHttpRequestException()
        {
            // Arrange
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.PatchSysMessage("sys-col", "chunk-id", "");

            // Assert
            await act.Should().ThrowAsync<BadHttpRequestException>();
        }

        // --- GetChatsCollection ---

        [Fact]
        public async Task GetChatsCollection_ValidSession_ReturnsCollectionPage()
        {
            // Arrange
            var expectedPage = new ChromaChatsCollection();

            _mockChatsRepo.Setup(r => r.GetCollection("chat-col")).ReturnsAsync(CreateChatsCollection(sessionId: "sess1"));
            _mockChatsRepo.Setup(r => r.GetCollectionPage("chat-col", It.IsAny<Dictionary<string, object>>(), false, 10, 0))
                          .ReturnsAsync(expectedPage);
            var sut = CreateSut();

            // Act
            var result = await sut.GetChatsCollection("chat-col", sessionId: "sess1");

            // Assert
            result.Should().Be(expectedPage);
        }

        [Fact]
        public async Task GetChatsCollection_UnknownSession_ThrowsBadHttpRequestException()
        {
            // Arrange
            _mockChatsRepo.Setup(r => r.GetCollection("chat-col")).ReturnsAsync(CreateChatsCollection(sessionId: "sess1"));
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.GetChatsCollection("chat-col", sessionId: "sess-unknown");

            // Assert
            await act.Should().ThrowAsync<BadHttpRequestException>()
                .WithMessage("*sess-unknown*");
        }

        // --- OnNewChatMessage ---

        [Fact]
        public async Task OnNewChatMessage_ExistingCollection_EmbedsAndReturnsMessage()
        {
            // Arrange
            var message = new ChatMessage("user", "Hello world");
            var defaultChunk = new ChromaChatChunk();

            _mockChatsRepo.Setup(r => r.CollectionExists("agent-user")).ReturnsAsync(true);
            _mockEmbeddingsService.Setup(e => e.GenerateEmbeddings(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>()))
                                  .ReturnsAsync(CreateModelEmbeddings());
            _mockChatsRepo.Setup(r => r.DefaultChunk("agent-user")).ReturnsAsync(defaultChunk);
            var sut = CreateSut();

            // Act
            var result = await sut.OnNewChatMessage(message, "agent-user");

            // Assert
            result.Should().Be(message);
        }

        [Fact]
        public async Task OnNewChatMessage_NewCollectionInvalidNameFormat_ThrowsInvalidDataException()
        {
            // Arrange
            var message = new ChatMessage("user", "Hello");

            _mockChatsRepo.Setup(r => r.CollectionExists("badname")).ReturnsAsync(false);
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.OnNewChatMessage(message, "badname");

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*FORMAT*");
        }

        // --- StoreSysChunk ---

        [Fact]
        public async Task StoreSysChunk_NewChunk_InsertsWithVersionV1()
        {
            // Arrange
            var chunk = new ChromaSysChunk { Name = "new-chunk", Tag = "base", Text = "System instruction" };
            var inserted = CreateSysChunk("new-chunk");

            _mockSysRepo.Setup(r => r.GetCollection("sys-col")).ReturnsAsync(CreateSysCollection());
            _mockSysRepo.Setup(r => r.DefaultChunk("sys-col")).ReturnsAsync(new ChromaSysChunk());
            _mockSysRepo.Setup(r => r.ExistsMessage("sys-col", "new-chunk", null)).ReturnsAsync(false);
            _mockEmbeddingsService.Setup(e => e.GenerateEmbeddings(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>()))
                                  .ReturnsAsync(CreateModelEmbeddings());
            _mockSysRepo.Setup(r => r.InsertChunk("sys-col", It.IsAny<ChromaSysChunk>(), It.IsAny<Dictionary<string, object>>()))
                        .ReturnsAsync(inserted);
            var sut = CreateSut();

            // Act
            var result = await sut.StoreSysChunk(chunk, "sys-col");

            // Assert
            result.Should().Be(inserted);
            _mockSysRepo.Verify(r => r.InsertChunk("sys-col", It.IsAny<ChromaSysChunk>(), It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task StoreSysChunk_EmptyCollectionName_ThrowsInvalidDataException()
        {
            // Arrange
            var chunk = new ChromaSysChunk { Name = "chunk", Tag = "base", Text = "text" };
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.StoreSysChunk(chunk, "");

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*COLLECTION*");
        }

        // --- SimilaritySearch ---

        [Fact]
        public async Task SimilaritySearch_ValidQuery_ReturnsMappedResults()
        {
            // Arrange
            var queryChunks = new List<ChromaQueryChunk>
            {
                new("c1", 0.9, "text1", new ReadOnlyMemory<float>(new[] { 0.1f }),
                    new Dictionary<string, object> { ["document_name"] = "doc1", ["model"] = "m", ["dimensions"] = 512, ["type"] = 1 }),
                new("c2", 0.8, "text2", new ReadOnlyMemory<float>(new[] { 0.2f }),
                    new Dictionary<string, object> { ["document_name"] = "doc2", ["model"] = "m", ["dimensions"] = 512, ["type"] = 1 })
            };

            _mockRepo.Setup(r => r.QueryCollection("test-col", It.IsAny<ReadOnlyMemory<float>>(), 2, It.IsAny<Dictionary<string, object>>()))
                     .ReturnsAsync(queryChunks);
            var sut = CreateSut();

            // Act
            var result = await sut.SimilaritySearch("test-col", new ReadOnlyMemory<float>(new[] { 0.5f }), 2, new Dictionary<string, object>());

            // Assert
            result.Should().HaveCount(2);
            result[0].Text.Should().Be("text1");
            result[0].SearchIndex.Should().Be("test-col");
            result[0].Document.Should().Be("doc1");
        }

        // --- Delegation happy paths ---

        [Fact]
        public async Task DeleteCollection_ValidName_ReturnsTrue()
        {
            // Arrange
            _mockRepo.Setup(r => r.DeleteCollection("test-col")).ReturnsAsync(true);
            var sut = CreateSut();

            // Act
            var result = await sut.DeleteCollection("test-col");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllFileCollections_DelegatesToFileMgmtService()
        {
            // Arrange
            var expected = new List<ChromaFilesCollection>();
            _mockFileMgmtService.Setup(s => s.GetAllCollections()).ReturnsAsync(expected);
            var sut = CreateSut();

            // Act
            var result = await sut.GetAllFileCollections();

            // Assert
            result.Should().BeSameAs(expected);
        }
    }
}
