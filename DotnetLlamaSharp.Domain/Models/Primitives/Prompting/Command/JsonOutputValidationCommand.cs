using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using OllamaSharp.Models;
using System.Text.Json;
using System.Text.Json.Schema;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class JsonOutputValidationCommand<TModel> : ChromaPromptCommand<ScoredBoolResponse>
    {
        public JsonOutputValidationCommand() {}

        public JsonOutputValidationCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) 
            : base(repo, dbMessageName, guidanceMessage, settings) {}

        public override async Task<ScoredBoolResponse> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            validateInputRequest<JsonValidationRequest<TModel>>(request);

            var promptRequest = (JsonValidationRequest<TModel>)request;

            var cmdRequest = await getGenerateRequest(request);

            cmdRequest.System = cmdRequest.System
                .Replace("<<PROMPT>>", $"- instruction: {promptRequest.SystemMessage}\n- input:{request.Prompt}")
                .Replace("<<RESPONSE>>", promptRequest.RawOutput)
                .Replace("<<SCHEMA>>", JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(TModel)).ToJsonString());
 
            return await ollama.StructuredPrompt<ScoredBoolResponse>(cmdRequest);
        }
        public override Task<ScoredBoolResponse> PromptSync(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            if (request.GetType() != typeof(JsonValidationRequest<TModel>))
                throw new InvalidOperationException($"{nameof(JsonOutputValidationCommand<TModel>)} >> INVALID REQUEST TYPE ({request.GetType().Name}) IS NOT OF TYPE {nameof(JsonValidationRequest<TModel>)}");

            var promptRequest = (JsonValidationRequest<TModel>)request;

            var cmdRequest = getGenerateRequest(request).Result;

            cmdRequest.System = cmdRequest.System
                .Replace("<<PROMPT>>", $"- instruction: {promptRequest.SystemMessage}\n- input:{request.Prompt}")
                .Replace("<<RESPONSE>>", promptRequest.RawOutput)
                .Replace("<<SCHEMA>>", JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(TModel)).ToJsonString());

            return ollama.StructuredPrompt<ScoredBoolResponse>(cmdRequest);
        }

        protected override string getDefaultInstruction() => @"
You are going to be provided a USER PROMPT, a JSON RESPONSE for that prompt and a EXPECTED OUTPUT SCHEMA for that response.
Your task is to validate the provided JSON RESPONSE and determine if it is compliant with the user request and the expected response according to the provided EXPECTED OUTPUT SCHEMA.

You must output your response in JSON format according to this fields:

- Answer: boolean value to indicate 'VALID' or 'NOT VALID' according to the result of your deliberation.
- Justification: a brief text (not more than 10 words) summarizing the reason of your boolean answer.
- Confidence: a float value ranging from 0.0 to 1.0 to indicate how sure you are about your answer.

# IMPORTANT: follow this steps in order to generate your response:

> STEP 1: Analyze the user request and try understand the intent to get an idea of what is the user expecting to receive.
> STEP 2: Analyze the provided EXPECTED OUTPUT SCHEMA to get a clear idea of what is the expected result in terms of format.
> STEP 3: Analyze the provided JSON RESPONSE that you must review and reason if it is valid in terms of content and consistent with the user intent.
> STEP 4: Use your conclussions of the previous steps to generate your response according to the requested JSON schema.

# RULES: take into account this rules when generating your final response:

- Ensure your output is compliant with the requested schema for your validation. 
- Ensure that the format of the JSON RESPONSE is valid to be serialized to a C# class.

# USER PROMPT: 

<<PROMPT>>

# JSON RESPONSE:

<<RESPONSE>>

# EXPECTED OUTPUT SCHEMA:

<<SCHEMA>>
";
    }
}
