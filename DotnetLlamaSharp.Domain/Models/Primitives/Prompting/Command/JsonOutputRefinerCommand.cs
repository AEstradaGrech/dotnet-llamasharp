using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command
{
    public class JsonOutputRefinerCommand<TRefined> : ChromaPromptCommand<TRefined> where TRefined : class
    {
        public JsonOutputRefinerCommand() { }

        public JsonOutputRefinerCommand(IChromaSysChunksRepository repo, string dbMessageName, string? guidanceMessage = null, CommandSettings? settings = null) : base(repo, dbMessageName, guidanceMessage, settings) { }

        public override async Task<TRefined> Prompt(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            validateInputRequest<JsonValidationRequest<TRefined>>(request);

            var validationReq = (JsonRefineRequest<TRefined>)request;

            ScoredBoolResponse boolValidation = null;
            switch(validationReq.ValidationType)
            {
                case (EPromptValidation.REVIEW_ONLY):
                    return await reviewResponse(_repo, ollama, validationReq);

                case (EPromptValidation.BOOL_AND_RETRY):
                    boolValidation = await validateResponse(_repo, ollama, validationReq);

                    if (!boolValidation.Answer)
                        throw new InvalidDataException($"{nameof(JsonOutputRefinerCommand<TRefined>)} >> {nameof(validateResponse)} >> VALIDATION FAIL - REASON: {boolValidation.Justification} >> CONFIDENCE: {boolValidation.Score}");

                    else return JsonSerializer.Deserialize<TRefined>(validationReq.RawOutput);
                
                case (EPromptValidation.BOOL_AND_REVIEW):
                    return await validateAndReview(_repo, ollama, validationReq);

                case (EPromptValidation.DOUBLE_BOOL):
                    return await doubleBool(_repo, ollama, validationReq);

                default: return null;
            }
        }

        public override Task<TRefined> PromptSync(IOllamaInferenceService ollama, PromptCommandRequest request)
        {
            validateInputRequest<JsonValidationRequest<TRefined>>(request);

            var validationReq = (JsonRefineRequest<TRefined>)request;

            ScoredBoolResponse boolValidation = null;
            switch (validationReq.ValidationType)
            {
                case (EPromptValidation.REVIEW_ONLY):
                    return reviewResponse(_repo, ollama, validationReq);

                case (EPromptValidation.BOOL_AND_RETRY):
                    boolValidation = validateResponse(_repo, ollama, validationReq).Result;

                    if (!boolValidation.Answer)
                        throw new InvalidDataException($"{nameof(JsonOutputRefinerCommand<TRefined>)} >> {nameof(validateResponse)} >> VALIDATION FAIL - REASON: {boolValidation.Justification} >> CONFIDENCE: {boolValidation.Score}");

                    else return Task.FromResult(JsonSerializer.Deserialize<TRefined>(validationReq.RawOutput));

                case (EPromptValidation.BOOL_AND_REVIEW):
                    return validateAndReview(_repo, ollama, validationReq);

                case (EPromptValidation.DOUBLE_BOOL):
                    return doubleBool(_repo, ollama, validationReq);

                default: return null;
            }
        }

        private Task<TRefined> reviewResponse(IChromaSysChunksRepository repo, IOllamaInferenceService ollama, JsonRefineRequest<TRefined> request, string? guidanceMessage = null)
        {
            var command = new JsonOutputReviewCommand<TRefined>(repo, "json-review", guidanceMessage, _settings);

            return command.PromptSync(ollama, toValidationRequest<TRefined>(request));
        }

        private Task<ScoredBoolResponse> validateResponse(IChromaSysChunksRepository repo, IOllamaInferenceService ollama, JsonRefineRequest<TRefined> request, string? guidanceMessage = null)
        {
            var command = new JsonOutputValidationCommand<ScoredBoolResponse>(repo, "json-validate", guidanceMessage, _settings);

            return command.PromptSync(ollama, toValidationRequest<ScoredBoolResponse>(request));
        }

        private Task<TRefined> validateAndReview(IChromaSysChunksRepository repo, IOllamaInferenceService ollama, JsonRefineRequest<TRefined> request, string? guidanceMessage = null)
        {
            var validation = validateResponse(_repo, ollama, request).Result;

            if (validation.Answer) 
                return Task.FromResult(JsonSerializer.Deserialize<TRefined>(request.RawOutput));

            return reviewResponse(_repo, ollama, request, $"# WARNING: a previous reviewer has marked the response as INVALID. Take into account the reason to have a better understanding of the problem. Reason: {validation.Justification}");
        }
        private Task<TRefined> doubleBool(IChromaSysChunksRepository repo, IOllamaInferenceService ollama, JsonRefineRequest<TRefined> request, string? guidanceMessage = null)
        {
            var review = validateAndReview(repo, ollama, request, guidanceMessage).Result;

            request.RawOutput = JsonSerializer.Serialize<TRefined>(review);

            var validation = validateResponse(_repo, ollama, request).Result;

            if(!validation.Answer)
                throw new InvalidDataException($"{nameof(JsonOutputRefinerCommand<TRefined>)} >> {nameof(validateResponse)} >> VALIDATION FAIL - REASON: {validation.Justification} >> CONFIDENCE: {validation.Score}");

            return Task.FromResult(review);
        }

        JsonValidationRequest<TValResult> toValidationRequest<TValResult>(JsonRefineRequest<TRefined> request)
            => new JsonValidationRequest<TValResult> {
                Model = request.Model,
                Prompt = request.Prompt,
                SystemMessage = request.SystemMessage,
                RawOutput = request.RawOutput,
                Settings = request.Settings,
                ResponseExamples = request.ResponseExamples
            };
    }
}
