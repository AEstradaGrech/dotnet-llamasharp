using OllamaSharp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class MultiChoiceRequest : StringChoiceRequest
    {
        public MultiChoiceRequest() { }
        public MultiChoiceRequest(int selections, List<string> choices, string message, RequestOptions? settings, string? model = null) : base(choices, message, settings, model)
        {
            MaxSelections = selections;
        }
        public int MaxSelections { get; set; }
    }
}
