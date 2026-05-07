using OllamaSharp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class PromptCommandRequest
    {
        public string Prompt { get; set; }
        public string? Model { get; set; }
        public RequestOptions? Settings { get; set; }
    }
}
