using Dotnet.OllamaSharp.LameChain.SDK.Commands.Core.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;
using Dotnet.OllamaSharp.LameChain.SDK.Interfaces.Command.Services;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Services.Prompting;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OllamaSharp.Models;

namespace Dotnet.LlamaSharp.Tests.Service
{
    public class OllamaSharpServiceTests
    {
        private readonly Mock<IPromptCommandsFactory> _mockFactory;
        private readonly Mock<IOptions<RequestOptions>> _mockOptions;
        private readonly Mock<ILogger<OllamaSharpService>> _mockLogger;
        private readonly Mock<MessagePromptCommand> _mockCommand;

        public OllamaSharpServiceTests()
        {
            _mockFactory = new Mock<IPromptCommandsFactory>();
            _mockOptions = new Mock<IOptions<RequestOptions>>();
            _mockLogger = new Mock<ILogger<OllamaSharpService>>();
            _mockCommand = new Mock<MessagePromptCommand>();

            _mockOptions.Setup(o => o.Value).Returns(new RequestOptions());
        }

        private OllamaSharpService CreateSut() =>
            new OllamaSharpService(_mockFactory.Object, _mockOptions.Object, _mockLogger.Object);

        private static SimpleCommandRequest SimpleRequest(string prompt = "Hello", string sysmsg = "Be helpful") =>
            new SimpleCommandRequest
            {
                Prompt = prompt,
                SystemMessage = sysmsg,
                Settings = new CommandSettings("llama3.2")
            };

        private static CommandChatRequest ChatRequest(string prompt = "Hello", string sysmsg = "Be helpful") =>
            new CommandChatRequest
            {
                Prompt = prompt,
                SystemMessage = sysmsg,
                Settings = new CommandSettings("llama3.2"),
                ChatHistory = new List<ChatMessage> { new ChatMessage("user", "prior message") }
            };

        // --- Constructor ---

        [Fact]
        public void Constructor_NullFactory_ThrowsArgumentNullException()
        {
            // Arrange / Act
            var act = () => new OllamaSharpService(null!, _mockOptions.Object, _mockLogger.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        // --- SimplePrompt ---

        [Fact]
        public async Task SimplePrompt_ValidRequest_ReturnsChatPromptWithModelAndOutput()
        {
            // Arrange
            var request = SimpleRequest();
            _mockFactory.Setup(f => f.GetMessagePromptCommand(request.SystemMessage, request.Settings))
                        .Returns(_mockCommand.Object);
            _mockCommand.Setup(c => c.Prompt(It.IsAny<PromptCommandRequest>()))
                        .ReturnsAsync(new ChatMessage("assistant", "Hello back"));
            var sut = CreateSut();

            // Act
            var result = await sut.SimplePrompt(request);

            // Assert
            result.Model.Should().Be("llama3.2");
            result.Input.Should().Be("Hello");
            result.Output.Should().Be("Hello back");
            result.ChatHistory.Should().HaveCount(2);
        }

        [Fact]
        public async Task SimplePrompt_EmptySystemMessage_SetsDefaultSystemMessage()
        {
            // Arrange
            var request = SimpleRequest(sysmsg: "");
            _mockFactory.Setup(f => f.GetMessagePromptCommand(It.IsAny<string>(), request.Settings))
                        .Returns(_mockCommand.Object);
            _mockCommand.Setup(c => c.Prompt(It.IsAny<PromptCommandRequest>()))
                        .ReturnsAsync(new ChatMessage("assistant", "response"));
            var sut = CreateSut();

            // Act
            await sut.SimplePrompt(request);

            // Assert
            _mockFactory.Verify(f => f.GetMessagePromptCommand("You are a helpful assistant", request.Settings), Times.Once);
        }

        [Fact]
        public async Task SimplePrompt_NullResponseContent_ReturnsEmptyOutput()
        {
            // Arrange
            var request = SimpleRequest();
            _mockFactory.Setup(f => f.GetMessagePromptCommand(It.IsAny<string>(), It.IsAny<CommandSettings>()))
                        .Returns(_mockCommand.Object);
            _mockCommand.Setup(c => c.Prompt(It.IsAny<PromptCommandRequest>()))
                        .ReturnsAsync(new ChatMessage("assistant", null!));
            var sut = CreateSut();

            // Act
            var result = await sut.SimplePrompt(request);

            // Assert
            result.Output.Should().Be("");
        }

        // --- ChatPrompt ---

        [Fact]
        public async Task ChatPrompt_ValidRequest_ReturnsChatPromptWithOutput()
        {
            // Arrange
            var request = ChatRequest();
            _mockFactory.Setup(f => f.GetMessagePromptCommand(request.SystemMessage, request.Settings))
                        .Returns(_mockCommand.Object);
            _mockCommand.Setup(c => c.Prompt(It.IsAny<PromptCommandRequest>()))
                        .ReturnsAsync(new ChatMessage("assistant", "AI reply"));
            var sut = CreateSut();

            // Act
            var result = await sut.ChatPrompt(request, false);

            // Assert
            result.Model.Should().Be("llama3.2");
            result.Input.Should().Be("Hello");
            result.Output.Should().Be("AI reply");
        }

        [Fact]
        public async Task ChatPrompt_ValidRequest_AppendsResponseToChatHistory()
        {
            // Arrange
            var request = ChatRequest();
            _mockFactory.Setup(f => f.GetMessagePromptCommand(It.IsAny<string>(), It.IsAny<CommandSettings>()))
                        .Returns(_mockCommand.Object);
            _mockCommand.Setup(c => c.Prompt(It.IsAny<PromptCommandRequest>()))
                        .ReturnsAsync(new ChatMessage("assistant", "AI reply"));
            var sut = CreateSut();

            // Act
            var result = await sut.ChatPrompt(request, false);

            // Assert
            result.ChatHistory.Should().HaveCount(2);
            result.ChatHistory.Last().Content.Should().Be("AI reply");
            result.ChatHistory.Last().Role.Should().Be("assistant");
        }

        [Fact]
        public async Task ChatPrompt_WithSysmsgUpdate_PassesIncludeSystemMessageTrue()
        {
            // Arrange
            var request = ChatRequest();
            _mockFactory.Setup(f => f.GetMessagePromptCommand(It.IsAny<string>(), It.IsAny<CommandSettings>()))
                        .Returns(_mockCommand.Object);

            ChatCommandRequest? capturedChatRequest = null;
            _mockCommand.Setup(c => c.Prompt(It.IsAny<PromptCommandRequest>()))
                        .Callback<PromptCommandRequest>(r => capturedChatRequest = r as ChatCommandRequest)
                        .ReturnsAsync(new ChatMessage("assistant", "reply"));
            var sut = CreateSut();

            // Act
            await sut.ChatPrompt(request, bWithSysmsgUpdate: true);

            // Assert
            capturedChatRequest.Should().NotBeNull();
            capturedChatRequest!.IncludeSystemMessage.Should().BeTrue();
        }
    }
}
