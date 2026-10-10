using Dotnet.Chroma.Repositories;
using Dotnet.Chroma.Repositories.Interfaces;
using Dotnet.Chroma.Repositories.Models;
using Dotnet.Chroma.Repositories.Models.Client.Request;
using Dotnet.Chroma.Repositories.Models.Client.Response;
using Dotnet.Chroma.Repositories.Models.Settings;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Infrastructure.Repositories.Chroma;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Dotnet.LlamaSharp.Tests.Infrastructure
{
    // ChromaSysChunksRepository builds on top of ChromaRepository<TCol, TChunk>, which talks to Chroma
    // exclusively through IChromaDbClient. Mocking that interface directly (with Moq) is enough to
    // exercise the repository logic - no HTTP layer / WireMock is involved anymore.

    public class ChromaSysChunksRepositoryTests
    {
        private readonly Mock<IChromaDbClient> _mockClient;

        public ChromaSysChunksRepositoryTests()
        {
            _mockClient = new Mock<IChromaDbClient>();
        }

        private ChromaSysChunksRepository CreateSut()
            => new ChromaSysChunksRepository(
                NullLogger<ChromaRepository<SysChunksCollection, ChromaSysChunk>>.Instance,
                Options.Create(new ChromaSettings()),
                _mockClient.Object);

        // --- CreateCollection ---

        [Fact]
        public async Task CreateCollection_EmptyEmbedding_ThrowsInvalidOperationException()
        {
            // Arrange
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.CreateCollection("test-col", "test-col description", null);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*embedding length is 0*");
        }

        // --- ExistsMessage ---

        [Fact]
        public async Task ExistsMessage_ChunkFound_ReturnsTrue()
        {
            // Arrange
            SetupCollectionMock("sys-col", "col-abc");
            StubFilterDocuments("col-abc", "sys-msg",
                ids: ["1"],
                texts: ["The system message"],
                metadatas: [new Dictionary<string, object> { ["document_name"] = "sys-msg", ["chunk_type"] = 2 }]);
            var sut = CreateSut();

            // Act
            var result = await sut.ExistsMessage("sys-col", "sys-msg", null);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsMessage_ChunkNotFound_ReturnsFalse()
        {
            // Arrange
            SetupCollectionMock("sys-col", "col-abc");
            StubFilterDocuments("col-abc", "missing-msg", ids: [], texts: [], metadatas: []);
            var sut = CreateSut();

            // Act
            var result = await sut.ExistsMessage("sys-col", "missing-msg", null);

            // Assert
            result.Should().BeFalse();
        }

        // --- GetByName ---

        [Fact]
        public async Task GetByName_ChunkFound_ReturnsChunkWithHighestId()
        {
            // Arrange - GetByName sin versión ordena por ID numérico descendente y retorna el primero
            SetupCollectionMock("sys-col", "col-abc");
            StubFilterDocuments("col-abc", "sys-msg",
                ids: ["1", "2", "3"],
                texts: ["v1", "v2", "v3"],
                metadatas:
                [
                    new Dictionary<string, object> { ["document_name"] = "sys-msg", ["chunk_type"] = 2 },
                    new Dictionary<string, object> { ["document_name"] = "sys-msg", ["chunk_type"] = 2 },
                    new Dictionary<string, object> { ["document_name"] = "sys-msg", ["chunk_type"] = 2 }
                ]);
            var sut = CreateSut();

            // Act
            var result = await sut.GetByName("sys-col", "sys-msg");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("3");
        }

        [Fact]
        public async Task GetByName_ChunkNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            SetupCollectionMock("sys-col", "col-abc");
            StubFilterDocuments("col-abc", "missing-chunk", ids: [], texts: [], metadatas: []);
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.GetByName("sys-col", "missing-chunk");

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*missing-chunk*");
        }

        // --- DeleteByName ---

        [Fact]
        public async Task DeleteByName_ChunkFound_ReturnsDeletedChunk()
        {
            // Arrange - DeleteChunk verifica el borrado con el 'deletedCount' devuelto por DeleteDocuments.
            // updateCollectionChunksCount (disparado cuando el borrado tiene éxito) necesita el stub de upsert.
            SetupCollectionMock("sys-col", "col-abc");
            StubFilterDocuments("col-abc", "sys-msg",
                ids: ["1"],
                texts: ["The system message"],
                metadatas: [new Dictionary<string, object> { ["document_name"] = "sys-msg", ["chunk_type"] = 2 }]);
            StubDeleteDocuments("col-abc", "1");
            StubUpsertDocument("col-abc");
            var sut = CreateSut();

            // Act
            var result = await sut.DeleteByName("sys-col", "sys-msg");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("1");
        }

        [Fact]
        public async Task DeleteByName_ChunkNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            SetupCollectionMock("sys-col", "col-abc");
            StubFilterDocuments("col-abc", "missing-msg", ids: [], texts: [], metadatas: []);
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.DeleteByName("sys-col", "missing-msg");

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*missing-msg*");
        }

        // --- GetSystemMessage ---

        [Fact]
        public async Task GetSystemMessage_ChunkFound_ReturnsMessageText()
        {
            // Arrange
            SetupCollectionMock("sys-col", "col-abc");
            StubFilterDocuments("col-abc", "sys-prompt",
                ids: ["1"],
                texts: ["You are a helpful assistant."],
                metadatas: [new Dictionary<string, object> { ["document_name"] = "sys-prompt", ["chunk_type"] = 2 }]);
            var sut = CreateSut();

            // Act
            var result = await sut.GetSystemMessage("sys-col", "sys-prompt");

            // Assert
            result.Should().Be("You are a helpful assistant.");
        }

        [Fact]
        public async Task GetSystemMessage_ChunkNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            SetupCollectionMock("sys-col", "col-abc");
            StubFilterDocuments("col-abc", "missing-prompt", ids: [], texts: [], metadatas: []);
            var sut = CreateSut();

            // Act
            Func<Task> act = () => sut.GetSystemMessage("sys-col", "missing-prompt");

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*missing-prompt*");
        }

        // --- Mock helpers ---

        // Sets up IChromaDbClient so that ChromaRepository.GetCollection("collectionName") succeeds,
        // returning the collection chunk (ID "0") metadata.
        private void SetupCollectionMock(string collectionName, string collectionId)
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
                        new Dictionary<string, object> { ["document_name"] = collectionName, ["chunk_type"] = 2 }
                    }
                });
        }

        // Stubs FilterDocuments for a metadata filter matching document_name = chunkName (used by GetChunks).
        private void StubFilterDocuments(string collectionId, string chunkName, string[] ids, string[] texts, Dictionary<string, object>[] metadatas)
        {
            _mockClient
                .Setup(c => c.FilterDocuments(
                    collectionId,
                    It.Is<Dictionary<string, object>>(f => f.ContainsKey("document_name") && (string)f["document_name"] == chunkName),
                    false, null, null, null, null))
                .ReturnsAsync(new ChromaDocumentModel
                {
                    Ids = ids.ToList(),
                    Documents = texts.ToList(),
                    Embeddings = new List<float[]>(),
                    Metadatas = metadatas.ToList()
                });
        }

        private void StubDeleteDocuments(string collectionId, string chunkId)
        {
            _mockClient
                .Setup(c => c.DeleteDocuments(collectionId, It.Is<List<string>>(ids => ids.Count == 1 && ids[0] == chunkId)))
                .ReturnsAsync(1);
        }

        private void StubUpsertDocument(string collectionId)
        {
            _mockClient
                .Setup(c => c.UpsertDocument(collectionId, It.IsAny<ChromaClientUpsertRequest>()))
                .ReturnsAsync(true);
        }
    }
}
