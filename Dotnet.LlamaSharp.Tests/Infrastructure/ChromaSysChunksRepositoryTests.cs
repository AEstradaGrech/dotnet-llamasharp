using Dotnet.Chroma.Repositories;
using Dotnet.Chroma.Repositories.Models;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Infrastructure.Repositories.Chroma;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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
    // requestDocuments hace POST a {ServerUrl}/api/v1/collections/{id}/get con filtros de metadatos.
    // Las llamadas a GetCollectionAsync y ListCollectionsAsync son métodos de IChromaClient y se mockean con Moq.
    // Las llamadas HTTP directas se interceptan con WireMock.Net.

    public class ChromaSysChunksRepositoryTests : IDisposable
    {
        private readonly Mock<IChromaClient> _mockClient;
        private WireMockServer? _wireMock;

        public ChromaSysChunksRepositoryTests()
        {
            _mockClient = new Mock<IChromaClient>();
        }

        public void Dispose()
        {
            _wireMock?.Stop();
            _wireMock?.Dispose();
            _wireMock = null;
        }

        private ChromaSysChunksRepository CreateSut()
            => new ChromaSysChunksRepository(
                NullLogger<ChromaRepository<SysChunksCollection, ChromaSysChunk>>.Instance,
                Options.Create(new ChromaSettings()),
                _mockClient.Object);

        private ChromaSysChunksRepository CreateSutWithWireMock()
            => new ChromaSysChunksRepository(
                NullLogger<ChromaRepository<SysChunksCollection, ChromaSysChunk>>.Instance,
                Options.Create(new ChromaSettings { ServerUrl = _wireMock!.Url! }),
                _mockClient.Object);

        // --- CreateCollection ---

        [Fact]
        public async Task CreateCollection_EmptyEmbedding_ThrowsInvalidOperationException()
        {
            // Arrange
            var sut = CreateSut();
            var emptyEmbedding = new ReadOnlyMemory<float>(Array.Empty<float>());

            // Act
            Func<Task> act = () => sut.CreateCollection("test-col", emptyEmbedding, null);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*embedding length is 0*");
        }

        // --- ExistsMessage ---

        [Fact]
        public async Task ExistsMessage_ChunkFound_ReturnsTrue()
        {
            // Arrange
            SetupChromaDbMocks("sys-col", "col-abc");
            StubGetChunksByFilter("col-abc", "sys-msg",
                ids: ["1"],
                texts: ["The system message"],
                metadatas: [new Dictionary<string, object> { ["document_name"] = "sys-msg", ["chunk_type"] = 2 }]);
            var sut = CreateSutWithWireMock();

            // Act
            var result = await sut.ExistsMessage("sys-col", "sys-msg", null);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsMessage_ChunkNotFound_ReturnsFalse()
        {
            // Arrange
            SetupChromaDbMocks("sys-col", "col-abc");
            StubGetChunksByFilter("col-abc", "missing-msg", ids: [], texts: [], metadatas: []);
            var sut = CreateSutWithWireMock();

            // Act
            var result = await sut.ExistsMessage("sys-col", "missing-msg", null);

            // Assert
            result.Should().BeFalse();
        }

        // --- GetByName ---

        [Fact]
        public async Task GetByName_ChunkFound_ReturnsChunkWithHighestId()
        {
            // Arrange – GetByName sin versión ordena por ID numérico descendente y retorna el primero
            SetupChromaDbMocks("sys-col", "col-abc");
            StubGetChunksByFilter("col-abc", "sys-msg",
                ids: ["1", "2", "3"],
                texts: ["v1", "v2", "v3"],
                metadatas:
                [
                    new Dictionary<string, object> { ["document_name"] = "sys-msg", ["chunk_type"] = 2 },
                    new Dictionary<string, object> { ["document_name"] = "sys-msg", ["chunk_type"] = 2 },
                    new Dictionary<string, object> { ["document_name"] = "sys-msg", ["chunk_type"] = 2 }
                ]);
            var sut = CreateSutWithWireMock();

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
            SetupChromaDbMocks("sys-col", "col-abc");
            StubGetChunksByFilter("col-abc", "missing-chunk", ids: [], texts: [], metadatas: []);
            var sut = CreateSutWithWireMock();

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
            // Arrange – DeleteChunk verifica el borrado con GetChunkById justo después de DeleteEmbeddingsAsync.
            // El stub devuelve vacío para que chunk==null → bSucceeded=true.
            // updateCollectionChunksCount (disparado cuando bSucceeded=true) necesita el stub de upsert.
            SetupChromaDbMocks("sys-col", "col-abc");
            StubGetChunksByFilter("col-abc", "sys-msg",
                ids: ["1"],
                texts: ["The system message"],
                metadatas: [new Dictionary<string, object> { ["document_name"] = "sys-msg", ["chunk_type"] = 2 }]);
            StubChunkGetByIdEmpty("col-abc", "1");
            StubDeleteChunk("col-abc");
            StubUpsert("col-abc");
            var sut = CreateSutWithWireMock();

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
            SetupChromaDbMocks("sys-col", "col-abc");
            StubGetChunksByFilter("col-abc", "missing-msg", ids: [], texts: [], metadatas: []);
            var sut = CreateSutWithWireMock();

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
            SetupChromaDbMocks("sys-col", "col-abc");
            StubGetChunksByFilter("col-abc", "sys-prompt",
                ids: ["1"],
                texts: ["You are a helpful assistant."],
                metadatas: [new Dictionary<string, object> { ["document_name"] = "sys-prompt", ["chunk_type"] = 2 }]);
            var sut = CreateSutWithWireMock();

            // Act
            var result = await sut.GetSystemMessage("sys-col", "sys-prompt");

            // Assert
            result.Should().Be("You are a helpful assistant.");
        }

        [Fact]
        public async Task GetSystemMessage_ChunkNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            SetupChromaDbMocks("sys-col", "col-abc");
            StubGetChunksByFilter("col-abc", "missing-prompt", ids: [], texts: [], metadatas: []);
            var sut = CreateSutWithWireMock();

            // Act
            Func<Task> act = () => sut.GetSystemMessage("sys-col", "missing-prompt");

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*missing-prompt*");
        }

        // --- Mock / stub helpers ---

        // Inicia WireMock, mockea ListCollectionsAsync y GetCollectionAsync, y registra el stub HTTP
        // para el chunk de metadatos de colección (ID "0"), necesario cuando GetCollection es invocado.
        private void SetupChromaDbMocks(string collectionName, string collectionId)
        {
            _wireMock = WireMockServer.Start();

            _mockClient
                .Setup(c => c.ListCollectionsAsync(It.IsAny<CancellationToken>()))
                .Returns(AsyncEnumerable(collectionName));

            _mockClient
                .Setup(c => c.GetCollectionAsync(collectionName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChromaCollectionModel { Id = collectionId, Name = collectionName });

            // Stub para el chunk "0" (metadatos de colección), usado por GetCollection
            var collectionMetaJson = JsonSerializer.Serialize(new
            {
                ids = new[] { "0" },
                embeddings = (object?)null,
                documents = new[] { "" },
                metadatas = new[] { new Dictionary<string, object> { ["document_name"] = collectionName, ["chunk_type"] = 2 } }
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

        // Stub HTTP para GET por filtro de metadatos (document_name = chunkName).
        private void StubGetChunksByFilter(string collectionId, string chunkName,
            string[] ids, string[] texts, Dictionary<string, object>[] metadatas)
        {
            var json = JsonSerializer.Serialize(new
            {
                ids,
                embeddings = (object?)null,
                documents = texts,
                metadatas
            });

            _wireMock!
                .Given(Request.Create()
                    .WithPath($"/api/v1/collections/{collectionId}/get")
                    .UsingPost()
                    .WithBody(body => body != null && body.Contains($"\"{chunkName}\"")))
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(json));
        }

        // Stub HTTP para GET por ID de chunk que devuelve vacío.
        // DeleteChunk llama a GetChunkById una sola vez DESPUÉS de DeleteEmbeddingsAsync para verificar el borrado.
        // Devolver vacío hace que chunk==null → bSucceeded=true → DeleteChunk retorna true.
        private void StubChunkGetByIdEmpty(string collectionId, string chunkId)
        {
            var emptyJson = JsonSerializer.Serialize(new
            {
                ids = Array.Empty<string>(),
                embeddings = (object?)null,
                documents = Array.Empty<string>(),
                metadatas = Array.Empty<Dictionary<string, object>>()
            });

            _wireMock!
                .Given(Request.Create()
                    .WithPath($"/api/v1/collections/{collectionId}/get")
                    .UsingPost()
                    .WithBody(body => body != null && body.Contains($"\"{chunkId}\"")))
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(emptyJson));
        }

        // Stub HTTP para update de metadatos. updateCollectionChunksCount llama a requestEmbeddingsUpsert
        // con isCreate=false, que construye la URL como /api/v1/collections/{id}/update (no /upsert).
        private void StubUpsert(string collectionId)
        {
            _wireMock!
                .Given(Request.Create()
                    .WithPath($"/api/v1/collections/{collectionId}/update")
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody("{}"));
        }

        // Stub HTTP para DELETE y mock de IChromaClient.DeleteEmbeddingsAsync (cubre ambas rutas internas).
        private void StubDeleteChunk(string collectionId)
        {
            _mockClient
                .Setup(c => c.DeleteEmbeddingsAsync(collectionId, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _wireMock!
                .Given(Request.Create()
                    .WithPath($"/api/v1/collections/{collectionId}/delete")
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody("{}"));
        }

        private static async IAsyncEnumerable<string> AsyncEnumerable(params string[] items)
        {
            foreach (var item in items)
                yield return item;
        }
    }
}
