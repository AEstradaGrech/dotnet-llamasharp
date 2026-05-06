using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class SimplePromptRequest
    {
        public string Prompt { get; set; }
        public string SystemMessage { get; set; } = string.Empty;
        public string? Model { get; set; } = null;
        public int MaxTokens { get; set; } = 600;
        public float Temperature { get; set; } = 0.7f;
    }
}
