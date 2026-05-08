using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases
{
    public abstract class BasePromptCommand<T>
    {

        protected readonly string? _systemMessage = null;

        public string? SystemMessage => _systemMessage;
        
        public BasePromptCommand() { }
        public BasePromptCommand(string? systemMessage = null)
        {
            _systemMessage = systemMessage;
        }

        public abstract Task<T> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request);
        public async Task<T> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request, int retries = 0)
        {
            for(int i=0; i<retries+1; i++)
            {
                try
                {
                    return await Prompt(ollama, request);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            return default(T);
           
        }
        protected abstract Task<string> getPromptInstruction();
    }
}
