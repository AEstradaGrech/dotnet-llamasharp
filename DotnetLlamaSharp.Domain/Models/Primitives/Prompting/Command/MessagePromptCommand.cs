using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class MessagePromptCommand : BasePromptCommand<ChatMessage>
    {
        public MessagePromptCommand() : base() { }
        public MessagePromptCommand(string? systemMessage) : base(systemMessage) { }

        public override async Task<ChatMessage> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            bool isChat = request.GetType() == typeof(ChatCommandRequest);

            if (isChat)
                request = setRequestForChat((ChatCommandRequest)request);
                
            var message =  isChat ?
                await ollama.ChatPrompt(getChatRequest((ChatCommandRequest)request)) :
                await ollama.SimplePrompt(getGenerateRequest(request));

            return new ChatMessage(message.Role.ToString(), message.Content);
        }

        protected override async Task<string> getPromptInstruction() => _systemMessage == null ? string.Empty : _systemMessage;

        protected GenerateRequest getGenerateRequest(PromptCommandRequest request)
            => new GenerateRequest {
                Model = request.Model,
                Prompt = request.Prompt,
                System = _systemMessage,
                Stream = false,
                Options = request.Settings
            };
        
        protected ChatRequest getChatRequest(ChatCommandRequest request)
        {
            var messages = new List<Message>();

            request.ChatHistory.ForEach(x => messages.Add(new Message(new ChatRole(x.Role), x.Content)));

            return new ChatRequest
            {
                Model = request.Model,
                Messages = messages,
                Stream = false,
                Options = request.Settings
            };
        }

        protected ChatCommandRequest setRequestForChat(ChatCommandRequest request)
        {
            if (request.IncludeSystemMessage && !string.IsNullOrEmpty(_systemMessage))
                request.ChatHistory.Add(new ChatMessage(ChatRole.System.ToString(), _systemMessage));

            request.ChatHistory.Add(new ChatMessage(ChatRole.User.ToString(), request.Prompt));

            return request;
        }
    }
}
