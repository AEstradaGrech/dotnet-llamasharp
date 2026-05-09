using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class JsonValidationRequest<TModel> : PromptCommandRequest
    {
        public string RawOutput { get; set; }
        public string? ResponseExamples { get; set; } // o DB (para domain commands) o Task<Message> GenerateFewShotExamplesCommand<TModel>()
    }
}
