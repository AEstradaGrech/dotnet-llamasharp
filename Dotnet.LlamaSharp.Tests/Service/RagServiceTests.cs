using AutoMapper;
using Dotnet.Chroma.Repositories.Models;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;
using Dotnet.OllamaSharp.LameChain.SDK.Interfaces.Command.Services;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Domain.Services.Prompting;
using DotnetLlamaSharp.Infrastructure.Settings;
using DotnetLlamaSharp.Services.Prompting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OllamaSharp.Models;

namespace Dotnet.LlamaSharp.Tests.Service
{
    public class RagServiceTests
    {
        private readonly Mock<IOllamaSharpService> _mockChatService;
        private readonly Mock<IChromaService> _mockChromaService;
        private readonly Mock<IPromptCommandsService> _mockOllamaCommands;
        private readonly Mock<IPromptCommandsFactory> _mockFactory;
        private readonly Mock<IEmbeddingsService> _mockEmbeddingsService;
        private readonly Mock<IChromaChatsRepository> _mockChatsRepo;
        private readonly Mock<ILogger<RagService>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IOptions<ApiSettings>> _mockApiSettings;
        private readonly Mock<IOptions<RequestOptions>> _mockSettings;

        public RagServiceTests()
        {
            _mockChatService = new Mock<IOllamaSharpService>();
            _mockChromaService = new Mock<IChromaService>();
            _mockOllamaCommands = new Mock<IPromptCommandsService>();
            _mockFactory = new Mock<IPromptCommandsFactory>();
            _mockEmbeddingsService = new Mock<IEmbeddingsService>();
            _mockChatsRepo = new Mock<IChromaChatsRepository>();
            _mockLogger = new Mock<ILogger<RagService>>();
            _mockMapper = new Mock<IMapper>();
            _mockApiSettings = new Mock<IOptions<ApiSettings>>();
            _mockSettings = new Mock<IOptions<RequestOptions>>();

            _mockApiSettings.Setup(o => o.Value).Returns(new ApiSettings { DefaultModel = "llama3.2", DefaultUserName = "user", DefaultEmbedder = "nomic" });
            _mockSettings.Setup(o => o.Value).Returns(new RequestOptions());
        }

        private RagService CreateSut() =>
            new RagService(_mockChatService.Object, _mockChromaService.Object, _mockChatsRepo.Object,
                _mockOllamaCommands.Object, _mockFactory.Object, _mockEmbeddingsService.Object,
                _mockLogger.Object, _mockMapper.Object, _mockApiSettings.Object, _mockSettings.Object);

        private static RagCommandRequest BasicRagRequest() => new RagCommandRequest
        {
            Prompt = "What is RAG?",
            SystemMessage = "You are a helpful assistant.",
            Settings = new CommandSettings("llama3.2"),
            QueryCollections = new List<string> { "collection1" },
            CollectionRetrievals = 3
        };

        private static ChromaQuery BuildChromaQuery(string embModel, params (double dist, string text)[] chunks) =>
            new ChromaQuery
            {
                EmbeddingModel = embModel,
                Chunks = chunks.Select(c => new ChromaQueryChunk("id", c.dist, c.text, ReadOnlyMemory<float>.Empty, new Dictionary<string, object>())).ToList()
            };

        // --- QueryCollections ---

        [Fact]
        public async Task QueryCollections_SingleCollection_ReturnsQueryResultInDictionary()
        {
            // Arrange
            var chromaQuery = BuildChromaQuery("nomic", (0.2, "chunk text"));
            _mockChromaService.Setup(s => s.QueryCollection("collection1", "my text", 3, It.IsAny<Dictionary<string, object>>()))
                              .ReturnsAsync(chromaQuery);
            var sut = CreateSut();

            // Act
            var result = await sut.QueryCollections(new List<string> { "collection1" }, "my text", 3, null, new Dictionary<string, object>());

            // Assert
            result.Should().HaveCount(1);
            result["collection1"].Should().BeSameAs(chromaQuery);
        }

        [Fact]
        public async Task QueryCollections_MaxDistanceSet_ExcludesChunksAboveThreshold()
        {
            // Arrange
            var chromaQuery = BuildChromaQuery("nomic", (0.3, "close chunk"), (0.8, "far chunk"));
            _mockChromaService.Setup(s => s.QueryCollection(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Dictionary<string, object>>()))
                              .ReturnsAsync(chromaQuery);
            var sut = CreateSut();

            // Act
            var result = await sut.QueryCollections(new List<string> { "col" }, "text", 3, maxDistance: 0.5, new Dictionary<string, object>());

            // Assert
            result["col"].Chunks.Should().HaveCount(1);
            result["col"].Chunks.Single().Distance.Should().Be(0.3);
        }

        [Fact]
        public async Task QueryCollections_MaxDistanceZero_ReturnsAllChunks()
        {
            // Arrange
            var chromaQuery = BuildChromaQuery("nomic", (0.3, "a"), (0.9, "b"));
            _mockChromaService.Setup(s => s.QueryCollection(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Dictionary<string, object>>()))
                              .ReturnsAsync(chromaQuery);
            var sut = CreateSut();

            // Act
            var result = await sut.QueryCollections(new List<string> { "col" }, "text", 3, maxDistance: 0, new Dictionary<string, object>());

            // Assert
            result["col"].Chunks.Should().HaveCount(2);
        }

        [Fact]
        public async Task QueryCollections_EmptyNamesList_ReturnsEmptyDictionary()
        {
            // Arrange
            var sut = CreateSut();

            // Act
            var result = await sut.QueryCollections(new List<string>(), "text", 3, null, new Dictionary<string, object>());

            // Assert
            result.Should().BeEmpty();
            _mockChromaService.Verify(s => s.QueryCollection(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Dictionary<string, object>>()), Times.Never);
        }

        // --- SimpleRagQuery ---

        [Fact]
        public async Task SimpleRagQuery_ValidRequestWithCollection_ReturnsRagPromptWithOutput()
        {
            // Arrange
            var request = BasicRagRequest();
            var chromaQuery = BuildChromaQuery("nomic", (0.2, "relevant chunk"));
            var sysChunk = new ChromaSysChunk { Text = "Answer only from context." };
            var mappedRequest = new SimpleCommandRequest { Prompt = request.Prompt, Settings = request.Settings };
            var chatPrompt = new ChatPrompt { Model = "llama3.2", Input = "What is RAG?", Output = "RAG is retrieval-augmented generation.", ChatHistory = new List<ChatMessage>() };

            _mockChromaService.Setup(s => s.QueryCollection("collection1", It.IsAny<string>(), 3, It.IsAny<Dictionary<string, object>>()))
                              .ReturnsAsync(chromaQuery);
            _mockChromaService.Setup(s => s.GetSysMessage("system-messages", "rag-query", null))
                              .ReturnsAsync(sysChunk);
            _mockMapper.Setup(m => m.Map<RagCommandRequest, SimpleCommandRequest>(request))
                       .Returns(mappedRequest);
            _mockChatService.Setup(s => s.SimplePrompt(mappedRequest))
                            .ReturnsAsync(chatPrompt);
            var sut = CreateSut();

            // Act
            var result = await sut.SimpleRagQuery(request);

            // Assert
            result.Model.Should().Be("llama3.2");
            result.Input.Should().Be("What is RAG?");
            result.Output.Should().Be("RAG is retrieval-augmented generation.");
            result.EmbeddingModel.Should().Be("nomic");
            result.IncludedChunks.Should().HaveCount(1);
        }

        [Fact]
        public async Task SimpleRagQuery_NonEmptyOutput_AppendsAssistantMessageToHistory()
        {
            // Arrange
            var request = BasicRagRequest();
            var chromaQuery = BuildChromaQuery("nomic");
            var sysChunk = new ChromaSysChunk { Text = "" };
            var mappedRequest = new SimpleCommandRequest { Prompt = request.Prompt, Settings = request.Settings };
            var chatHistory = new List<ChatMessage>();
            var chatPrompt = new ChatPrompt { Model = "llama3.2", Input = "What is RAG?", Output = "answer", ChatHistory = chatHistory };

            _mockChromaService.Setup(s => s.QueryCollection(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Dictionary<string, object>>()))
                              .ReturnsAsync(chromaQuery);
            _mockChromaService.Setup(s => s.GetSysMessage("system-messages", "rag-query", null))
                              .ReturnsAsync(sysChunk);
            _mockMapper.Setup(m => m.Map<RagCommandRequest, SimpleCommandRequest>(request)).Returns(mappedRequest);
            _mockChatService.Setup(s => s.SimplePrompt(mappedRequest)).ReturnsAsync(chatPrompt);
            var sut = CreateSut();

            // Act
            var result = await sut.SimpleRagQuery(request);

            // Assert
            result.ChatHistory.Should().ContainSingle(m => m.Role == "assistant" && m.Content == "answer");
        }

        // --- RagChatPrompt validation ---

        [Fact]
        public async Task RagChatPrompt_EmptyPrompt_ThrowsBadHttpRequestException()
        {
            // Arrange
            var request = new RagChatCommandRequest { Prompt = "", Settings = new CommandSettings("llama3.2"), UserName = "alice", AgentName = "bot" };
            var sut = CreateSut();

            // Act
            var act = () => sut.RagChatPrompt(request);

            // Assert
            await act.Should().ThrowAsync<BadHttpRequestException>().WithMessage("*prompt*");
        }

        [Fact]
        public async Task RagChatPrompt_EmptyModelAndEmptyDefaultModel_ThrowsBadHttpRequestException()
        {
            // Arrange
            _mockApiSettings.Setup(o => o.Value).Returns(new ApiSettings { DefaultModel = "", DefaultUserName = "user" });
            var sut = CreateSut();
            var request = new RagChatCommandRequest { Prompt = "hello", Settings = new CommandSettings(""), AgentName = "", UserName = "alice" };

            // Act
            var act = () => sut.RagChatPrompt(request);

            // Assert
            await act.Should().ThrowAsync<BadHttpRequestException>().WithMessage("*Agent Name*");
        }

        [Fact]
        public async Task RagChatPrompt_EmptyUserNameAndEmptyDefaultUserName_ThrowsBadHttpRequestException()
        {
            // Arrange
            _mockApiSettings.Setup(o => o.Value).Returns(new ApiSettings { DefaultModel = "llama3.2", DefaultUserName = "" });
            var sut = CreateSut();
            var request = new RagChatCommandRequest { Prompt = "hello", Settings = new CommandSettings("llama3.2"), AgentName = "mybot", UserName = "" };

            // Act
            var act = () => sut.RagChatPrompt(request);

            // Assert
            await act.Should().ThrowAsync<BadHttpRequestException>().WithMessage("*user name*");
        }
    }
}
