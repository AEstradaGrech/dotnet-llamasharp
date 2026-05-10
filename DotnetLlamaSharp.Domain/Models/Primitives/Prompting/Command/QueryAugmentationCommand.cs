using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class QueryAugmentationCommand : ChromaPromptCommand<ChatMessage>
    {
        public QueryAugmentationCommand() { }

        public QueryAugmentationCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) : base(repo, dbMessageName, guidanceMessage, settings) { }

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
                var result = await getAugmentedQuery(ollama, promptReq);

                response.Content += $"{result}\n";

                if (expanseReq.Results > 0 && expanseReq.UsePrevAsExample && i < expanseReq.MaxExamples)
                    promptReq.System += $"\n\n{result}";
            }

            return response;
        }

        private async Task<string> getAugmentedQuery(IOllamaInferenceService ollama, GenerateRequest request)
        {
            var message = await ollama.SimplePrompt(request);

            return message.Content.Trim();
        }

        protected virtual string getDefaultInstruction() => @"Your task is to generate a variant of the user query that is similar in core and concept, but using different words or mentioning different related topics
so your response can be used to cover more points in a similarity search and retrieve more relevant data from the knowledge base.

Follow this steps in order to generate your response:

> STEP 1: Analyze the user query and extract the intent to get a better idea of the topic he is quering about.
> STEP 2: Extract the core topics of the user query to include them in your synthetized response.
> STEP 3: Use you conclussions from the previous steps to generate a cohesive response emulating the original user query.

Note: in case you are provided some EXPECTED OUTPUT EXAMPLES, use them to get an idea of your how your response should look like, but avoid repetition.

# USER QUERY:
";
    }
}
}
