using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using OllamaSharp.Models.Chat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChatChunk : ChromaChunk
    {
        const string roleTag = "- ROLE: ";
        const string messageSeparator = " >>";
        public ChromaChatChunk() : base() { }
        public ChromaChatChunk(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public ChromaChatChunk(string id, string text, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, text, embedding, metadata) { }

        public string EmbeddedText(string? agentName = null, string? userName = null)
            => string.IsNullOrEmpty(agentName) && string.IsNullOrEmpty(userName) ?
                Text.Replace(roleTag, "- ").Replace(messageSeparator, ":") :
                Text.Replace($"{roleTag}{ChatRole.Assistant.ToString()}", $"- {agentName}").Replace($"{roleTag}{ChatRole.User.ToString()}", $"- {userName}").Replace(messageSeparator, ":");
        
        protected override void setDefaultMetadata()
        {
            base.setDefaultMetadata();

            DefaultMetadata = JsonSerializer.Deserialize<ChatChunkMetadata>(JsonSerializer.Serialize(Metadata));
        }

        public void AppendMessage(ChatRole role, string message)
        {
            var meta = GetMeta<ChatChunkMetadata>();

            Text += $"\n\n{roleTag}{role.ToString()}{messageSeparator} {message.Trim()}";
            AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES).ToLower(), meta.TOTAL_MESSAGES + 1, resetDefault:true);
        }

        

        public void SetEmpty(bool isCurrent = false)
        {
            AddMetadata(nameof(ChatChunkMetadata.CHAT_INIT).ToLower(), false);
            AddMetadata(nameof(ChatChunkMetadata.CURRENT).ToLower(), isCurrent);
            AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES).ToLower(), 0);
            Text = string.Empty;
            Embedding = new ReadOnlyMemory<float>([]);
        }

        public List<ChatMessage> TextAsMessages()
        {
            if (string.IsNullOrEmpty(Text)) return [];

            var messages = new List<ChatMessage>();

            Text.Split("- ROLE:").ToList().ForEach(message =>
            {
                message = message.Trim();

                if(!string.IsNullOrEmpty(message))
                {
                    var split = message.Split(">>");

                    if(split.Length >= 2)
                    {
                        var role = split[0].Trim().ToLower();

                        messages.Add(new ChatMessage(role, split[1].Trim()));
                    }
                }
            });

            return messages;
        }
    }
}
