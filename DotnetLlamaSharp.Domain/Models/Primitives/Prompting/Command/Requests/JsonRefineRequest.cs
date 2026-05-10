using DotnetLlamaSharp.Domain.Models.Enums;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class JsonRefineRequest<TRefined> : JsonValidationRequest<TRefined> where TRefined : class
    {
        public TRefined Result { get; set; }
        public EPromptValidation ValidationType { get; set; }
    }
}
