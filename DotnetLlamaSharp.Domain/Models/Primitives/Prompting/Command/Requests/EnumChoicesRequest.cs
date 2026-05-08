using OllamaSharp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class EnumChoicesRequest<TEnum> : PromptCommandRequest where TEnum : struct, Enum
    {
        public EnumChoicesRequest() { }
        public EnumChoicesRequest(List<TEnum> choices, string message, RequestOptions? settings, string? model = null) : base(message, settings, model)
        {
            Choices = choices;
        }

        public List<TEnum> Choices { get; set; }
    }
}
