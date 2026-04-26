using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting
{
    public class ChatMessage
    {
        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public string Role { get; set; }
        public string Content { get; set; }
    }
}
