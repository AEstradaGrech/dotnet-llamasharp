using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class StringChoicesRequest : PromptCommandRequest
    {
        public List<string> Choices { get; set; }
    }
}
