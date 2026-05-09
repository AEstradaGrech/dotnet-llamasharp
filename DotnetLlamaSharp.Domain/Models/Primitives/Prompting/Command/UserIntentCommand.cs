using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class UserIntentCommand : MessagePromptCommand
    {
        public UserIntentCommand() : base() {}
        public UserIntentCommand(string? systemMessage = null, CommandSettings? settings = null) : base(systemMessage, settings) {}

        public override async Task<ChatMessage> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            var instruction = await getPromptInstruction();

            var systemMessage = string.IsNullOrEmpty(_systemMessage) ? instruction : $"{_systemMessage}.\n{instruction}"; 

            var chatReq = new ChatRequest
            {
                Model = request.Model,
                Messages = [new Message(ChatRole.System, systemMessage), new Message(ChatRole.User, request.Prompt)],
                Stream = false,
                Options = request.Settings
            };
            var response = await ollama.ChatPrompt(chatReq);

            return new ChatMessage(response.Role.ToString(), response.Content);
        }

        //TODO: if _dbMessageName == null, este message
        protected override async Task<string> getPromptInstruction()
            => @"Analyze the user prompt and try to determine the user intent. Pay attention to what is the user trying to do, what does the user expect you to do, 
what's the user query about or the scope of the user request (in case is chatting or role-playing) and output a short description (10 words maximum) of the user intent";
    }
}
