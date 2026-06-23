using Dotnet.Chroma.Repositories.Models;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Embedding;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.DocumentLoader;
using DotnetLlamaSharp.Domain.Models.Request.Chroma;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.DocumentLoader;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Infrastructure.Exceptions;
using DotnetLlamaSharp.Infrastructure.Services.DocumentLoaders;
using DotnetLlamaSharp.Infrastructure.Settings;
using DotnetLlamaSharp.Services.Embeddings;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

#pragma warning disable SKEXP0020

namespace Dotnet.LlamaSharp.Tests.Service
{
    public class ChromaFilesServiceTests
    {
        private readonly Mock<ILogger<ChromaFilesService>> _mockLogger;
        private readonly Mock<IChromaFilesRepository> _mockRepo;
        private readonly Mock<IDocumentLoader<PdfLoaderService>> _mockPdfLoader;
        private readonly Mock<IEmbeddingsService> _mockEmbeddingsService;
        private readonly IOptions<ApiSettings> _apiSettings;

        public ChromaFilesServiceTests()
        {
            _mockLogger = new Mock<ILogger<ChromaFilesService>>();
            _mockRepo = new Mock<IChromaFilesRepository>();
            _mockPdfLoader = new Mock<IDocumentLoader<PdfLoaderService>>();
            _mockEmbeddingsService = new Mock<IEmbeddingsService>();
            _apiSettings = Options.Create(new ApiSettings
            {
                DefaultEmbedder = "test-embedder",
                DefaultDimensions = 512
            });
        }

        private ChromaFilesService CreateSut() => new ChromaFilesService(
            _mockLogger.Object,
            _mockRepo.Object,
            _mockPdfLoader.Object,
            _mockEmbeddingsService.Object,
            _apiSettings);

        private static ChromaFilesCollection CreateCollection(string name = "test-col", string files = "", int totalChunks = 0)
        {
            return new ChromaFilesCollection("col-id", name, "desc", new Dictionary<string, object>
            {
                ["files"] = files,
                ["model"] = "test-embedder",
                ["dimensions"] = 512,
                ["total_chunks"] = totalChunks,
                ["chunk_type"] = (int)EChunkType.FILE,
                ["document_name"] = name,
                ["chunk_sizes"] = "",
                ["chunk_overlaps"] = "",
                ["skipped_pages"] = "",
                ["page_cutoffs"] = "",
                ["pages"] = "0",
                ["topics"] = ""
            });
        }

        private static ModelEmbeddings CreateModelEmbeddings()
        {
            var embedding = new TextEmbedding("text", new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f }), 2);
            return new ModelEmbeddings { Model = "test-embedder", GeneratedEmbeddings = [embedding] };
        }

        // --- CreateCollectionFromFile ---

        [Fact]
        public async Task CreateCollectionFromFile_NewCollection_ReturnsUpdatedCollection()
        {
            // Arrange – page text > chunkSize → splitAndChunk → 2 chunks (required to avoid index error in overlap logic)
            var page = new DocumentPage(1, "AAA BBB");
            var document = new Document("testfile", "path/testfile", 1, [page]);
            var emptyCollection = CreateCollection();
            var updatedCollection = CreateCollection();

            var request = new EmbedCollectionRequest
            {
                Name = "test-col",
                FileName = "testfile",
                FileExtension = "pdf",
                ChunkSize = 3,
                ChunkOverlap = 0,
                InitialSkip = 0,
                PageCutoff = 0,
                MinTextToChunk = 0,
                TopicTags = ["topic1"],
                Metadata = new Dictionary<string, object>(),
                EmbeddingModel = "test-embedder",
                Dimensions = 512
            };

            _mockPdfLoader.Setup(l => l.LoadDocument("testfile")).ReturnsAsync(document);
            _mockRepo.Setup(r => r.CollectionExists("test-col")).ReturnsAsync(false);
            _mockRepo.Setup(r => r.CreateCollection("test-col", It.IsAny<string>(), "test-embedder", 512, (int)EChunkType.FILE))
                     .ReturnsAsync(emptyCollection);
            _mockEmbeddingsService.Setup(e => e.GenerateEmbeddings(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>()))
                                  .ReturnsAsync(CreateModelEmbeddings());
            _mockRepo.Setup(r => r.InsertChunks("test-col", It.IsAny<List<ChromaChunk>>(), It.IsAny<Dictionary<string, object>>()))
                     .ReturnsAsync(2);
            _mockRepo.Setup(r => r.UpdateCollectionData("test-col", It.IsAny<Dictionary<string, object>>(), true))
                     .ReturnsAsync(updatedCollection);

            var sut = CreateSut();

            // Act
            var result = await sut.CreateCollectionFromFile(request);

            // Assert
            result.Should().Be(updatedCollection);
            _mockRepo.Verify(r => r.InsertChunks("test-col", It.Is<List<ChromaChunk>>(c => c.Count == 2), It.IsAny<Dictionary<string, object>>()), Times.Once);
            _mockRepo.Verify(r => r.UpdateCollectionData("test-col", It.IsAny<Dictionary<string, object>>(), true), Times.Once);
        }

        [Fact]
        public async Task CreateCollectionFromFile_PageCutoffExceedsPageCount_ThrowsInvalidOperationException()
        {
            // Arrange
            var page = new DocumentPage(1, "Hello World");
            var document = new Document("testfile", "path", 1, [page]);

            var request = new EmbedCollectionRequest
            {
                Name = "test-col",
                FileName = "testfile",
                FileExtension = "pdf",
                PageCutoff = 1, // >= Pages.Count (1) → invalid
                TopicTags = ["topic1"],
                Metadata = new Dictionary<string, object>(),
                EmbeddingModel = "test-embedder"
            };

            _mockPdfLoader.Setup(l => l.LoadDocument("testfile")).ReturnsAsync(document);
            _mockRepo.Setup(r => r.CollectionExists("test-col")).ReturnsAsync(false);
            _mockRepo.Setup(r => r.CreateCollection(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>()))
                     .ReturnsAsync(CreateCollection());

            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.CreateCollectionFromFile(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*cutoff*");
        }

        [Fact]
        public async Task CreateCollectionFromFile_FileAlreadyInCollection_ThrowsHttpException()
        {
            // Arrange
            var page = new DocumentPage(1, "Hello World");
            var document = new Document("testfile", "path", 1, [page]);
            var existingCollection = CreateCollection(files: "testfile"); // collection already has this file

            var request = new EmbedCollectionRequest
            {
                Name = "test-col",
                FileName = "testfile",
                FileExtension = "pdf",
                TopicTags = ["topic1"],
                Metadata = new Dictionary<string, object>(),
                EmbeddingModel = "test-embedder"
            };

            _mockPdfLoader.Setup(l => l.LoadDocument("testfile")).ReturnsAsync(document);
            _mockRepo.Setup(r => r.CollectionExists("test-col")).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetCollection("test-col")).ReturnsAsync(existingCollection);

            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.CreateCollectionFromFile(request);

            // Assert
            await act.Should().ThrowAsync<HttpException>()
                .Where(e => e.StatusCode == HttpStatusCode.BadRequest);
        }

        // --- CreateEmptyFileCollection ---

        [Fact]
        public async Task CreateEmptyFileCollection_ValidRequest_ReturnsNewCollection()
        {
            // Arrange
            var expectedCollection = CreateCollection("new-col");
            var request = new CreateCollectionRequest
            {
                Name = "new-col",
                Description = "New collection",
                EmbeddingModel = "test-embedder",
                Dimensions = 512
            };

            _mockRepo.Setup(r => r.GetCollection("new-col")).ReturnsAsync(expectedCollection);

            var sut = CreateSut();

            // Act
            var result = await sut.CreateEmptyFileCollection(request);

            // Assert
            result.Should().Be(expectedCollection);
        }

        [Fact]
        public async Task CreateEmptyFileCollection_RepoThrows_PropagatesException()
        {
            // Arrange
            var request = new CreateCollectionRequest { Name = "failing-col" };

            _mockRepo.Setup(r => r.CreateCollection(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>()))
                     .ThrowsAsync(new InvalidOperationException("repo error"));

            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.CreateEmptyFileCollection(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("repo error");
        }

        // --- InspectCollection ---

        [Fact]
        public async Task InspectCollection_ValidParameters_ReturnsInspectedCollection()
        {
            // Arrange
            var collection = CreateCollection(totalChunks: 10);
            var inspected = CreateCollection(totalChunks: 10);

            _mockRepo.Setup(r => r.GetCollection("test-col")).ReturnsAsync(collection);
            _mockRepo.Setup(r => r.InspectCollection("test-col", It.IsAny<List<string>>(), false))
                     .ReturnsAsync(inspected);

            var sut = CreateSut();

            // Act
            var result = await sut.InspectCollection("test-col", startIndex: 1, samples: 3, includeEmbeddings: false);

            // Assert
            result.Should().Be(inspected);
            _mockRepo.Verify(r => r.InspectCollection("test-col", It.Is<List<string>>(ids => ids.Count == 3 && ids[0] == "1" && ids[2] == "3"), false), Times.Once);
        }

        [Fact]
        public async Task InspectCollection_StartIndexExceedsTotalChunks_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = CreateCollection(totalChunks: 5);
            _mockRepo.Setup(r => r.GetCollection("test-col")).ReturnsAsync(collection);

            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.InspectCollection("test-col", startIndex: 6); // 6 > 5

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*grater*");
        }

        // --- GetAllCollections ---

        [Fact]
        public async Task GetAllCollections_ReturnsAllFileCollections()
        {
            // Arrange
            var collections = new List<ChromaFilesCollection>
            {
                CreateCollection("col1"),
                CreateCollection("col2")
            };

            _mockRepo.Setup(r => r.CollectionsOf((int)EChunkType.FILE)).ReturnsAsync(collections);

            var sut = CreateSut();

            // Act
            var result = await sut.GetAllCollections();

            // Assert
            result.Should().BeEquivalentTo(collections);
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllCollections_RepoReturnsEmpty_ReturnsEmptyList()
        {
            // Arrange
            _mockRepo.Setup(r => r.CollectionsOf((int)EChunkType.FILE))
                     .ReturnsAsync(new List<ChromaFilesCollection>());

            var sut = CreateSut();

            // Act
            var result = await sut.GetAllCollections();

            // Assert
            result.Should().BeEmpty();
        }
    }
}
