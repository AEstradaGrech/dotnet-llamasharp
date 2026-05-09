using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class BasePromptRequest
    {
        public string Prompt { get; set; }
        public string SystemMessage { get; set; } = string.Empty;
    }
}
