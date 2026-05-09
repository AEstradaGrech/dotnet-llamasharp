using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using System.Text.Json;
using System.Text.Json.Schema;

//Reviews and Rewrites / corrects result
namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class JsonOutputReviewCommand<TReviewed> : ChromaPromptCommand<TReviewed> where TReviewed : class
    {
        public JsonOutputReviewCommand() : base() {}

        public JsonOutputReviewCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null)
            : base(repo, dbMessageName, guidanceMessage, settings) { }

        public override async Task<TReviewed> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            if (request.GetType() != typeof(JsonValidationRequest<TReviewed>))
                throw new InvalidOperationException($"{nameof(JsonOutputReviewCommand<TReviewed>)} >> INVALID REQUEST TYPE ({request.GetType().Name}) IS NOT OF TYPE {nameof(JsonValidationRequest<TReviewed>)}");

            var promptRequest = (JsonValidationRequest<TReviewed>)request;

            var cmdRequest = await getGenerateRequest(request);

            cmdRequest.System = cmdRequest.System.Replace("<<PROMPT>>", request.Prompt).Replace("<<RESPONSE>>", promptRequest.RawOutput).Replace("<<SCHEMA>>", JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(TReviewed)).ToJsonString());

            return await ollama.StructuredPrompt<TReviewed>(cmdRequest);
        }

        public override Task<TReviewed> PromptSync(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            if (request.GetType() != typeof(JsonValidationRequest<TReviewed>))
                throw new InvalidOperationException($"{nameof(JsonOutputReviewCommand<TReviewed>)} >> INVALID REQUEST TYPE ({request.GetType().Name}) IS NOT OF TYPE {nameof(JsonValidationRequest<TReviewed>)}");

            var promptRequest = (JsonValidationRequest<TReviewed>)request;

            var cmdRequest = getGenerateRequest(request).Result;

            cmdRequest.System = cmdRequest.System.Replace("<<PROMPT>>", request.Prompt).Replace("<<RESPONSE>>", promptRequest.RawOutput).Replace("<<SCHEMA>>", JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(TReviewed)).ToJsonString());

            return ollama.StructuredPrompt<TReviewed>(cmdRequest);
        }

        protected override string getDefaultInstruction() => @"
You are going to be provided a USER PROMPT, a JSON RESPONSE for that prompt and a EXPECTED OUTPUT SCHEMA for that response.
Your task is to review the output, compare it with the expected response and return a corrected version of the JSON RESPONSE
or the original version if you consider it is fine.

# IMPORTANT: follow this steps in order to generate your response:

> STEP 1: Analyze the user request and try understand the intent to get an idea of what is the user expecting to receive.
> STEP 2: Analyze the provided EXPECTED OUTPUT SCHEMA to get a clear idea of what is the expected result in terms of format.
> STEP 3: Analyze the provided JSON RESPONSE that you must review and reason if it is valid in terms of content and consistent with the user intent.
> STEP 4: Take into account your analysis from the previous steps to reason and deliberate if the JSON RESPONSE is compliant with the user intent and the EXPECTED OUTPUT SCHEMA
> STEP 5: With the result of your deliberation of STEP 4, return the JSON RESPONSE if you think is compliant or a corrected version of it.

# RULES: take into account this rules when generating your final response:

- Ensure your output is compliant with the requested schema for your validation. 
- Ensure that the format of the JSON RESPONSE is valid to be serialized to a C# class.

# USER PROMPT: <<PROMPT>>

# JSON RESPONSE:

<<RESPONSE>>

# EXPECTED OUTPUT SCHEMA:

<<SCHEMA>>
";
    }
}
