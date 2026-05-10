using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class RagExpansionCommand : ChromaPromptCommand<ChatMessage>
    {
        public RagExpansionCommand() { }

        public RagExpansionCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) : base(repo, dbMessageName, guidanceMessage, settings) { }

        public override async Task<ChatMessage> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            validateInputRequest<RagExpansionRequest>(request);

            var expanseReq = (RagExpansionRequest)request;

            var promptReq = await getGenerateRequest(request);

            promptReq.System += $"\n{request.Prompt}";

            if (expanseReq.UsePrevAsExample)
                promptReq.System += $"\n\n# EXPECTED OUTPUT EXAMPLES:\n";

            var response = new ChatMessage(ChatRole.Assistant.ToString(), string.Empty);

            for (int i = 0; i < expanseReq.Results; i++)
            {
                var result = await getExpansionResponse(ollama, promptReq);

                response.Content += $"{result}\n";

                if (expanseReq.Results > 0 && expanseReq.UsePrevAsExample && i < expanseReq.MaxExamples)
                    promptReq.System += $"\n\n{result}";
            }

            return response;
        }

        private async Task<string> getExpansionResponse(IOllamaInferenceService ollama, GenerateRequest request)
        {
            var message = await ollama.SimplePrompt(request);

            return message.Content.Trim();
        }

        protected virtual string getDefaultInstruction() => @"Your task is to answer the user question in a brief yet accurate manner. The resulting text will be used to perform a similarity search and 
retrieve more data related to the user query so follow this steps in order to generate your response:

> STEP 1: Analyze the user query and extract the intent to get a better idea of the topic he is quering about.
> STEP 2: Extract the core topics of the user query to include them in your synthetized response.
> STEP 3: Use you conclussions from the previous steps to generate a cohesive response as brief as possible providing a general explanation of the core topics detected in the user query.

Note: in case you are provided some EXPECTED OUTPUT EXAMPLES, use them to get an idea of your how your response should look like, but avoid repetition.

# USER QUERY:
";
    }
}
