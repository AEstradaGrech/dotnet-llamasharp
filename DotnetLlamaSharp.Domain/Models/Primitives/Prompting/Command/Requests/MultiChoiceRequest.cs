using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class MultiChoiceRequest : StringChoiceRequest
    {
        public int MaxSelections { get; set; }
    }
}
