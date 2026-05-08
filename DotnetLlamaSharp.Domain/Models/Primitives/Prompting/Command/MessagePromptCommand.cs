using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class MessagePromptCommand : BasePromptCommand<ChatMessage>
    {
        public MessagePromptCommand(string? systemMessage) : base(systemMessage) { }

        public override async Task<ChatMessage> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            var message = request .GetType() == typeof(ChatCommandRequest) ?
                await ollama.ChatPrompt(getChatRequest((ChatCommandRequest)request)) :
                await ollama.SimplePrompt(getGenerateRequest(request));

            return new ChatMessage(message.Role.ToString(), message.Content);
        }

        protected override async Task<string> getPromptInstruction() => _systemMessage == null ? string.Empty : _systemMessage;

        private GenerateRequest getGenerateRequest(PromptCommandRequest request)
            => new GenerateRequest {
                Model = request.Model,
                Prompt = request.Prompt,
                System = _systemMessage,
                Stream = false,
                Options = request.Settings
            };
        
        private ChatRequest getChatRequest(ChatCommandRequest request)
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
    }
}
