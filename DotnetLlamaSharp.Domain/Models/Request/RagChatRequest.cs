using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class RagChatRequest : RagPromptRequest
    {
        public string? AgentName { get; set; }
        public string? UserName { get; set; }
        public bool IsNewSession { get; set; }
    }
}
