using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using OllamaSharp.Models.Chat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChatChunk : ChromaChunk
    {
        public ChromaChatChunk() { }
        public ChromaChatChunk(string id, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, embedding, metadata) { }
        public ChromaChatChunk(string id, string text, Dictionary<string, object> metadata) : base(id, text, metadata) { }
       
        protected override void setDefaultMetadata()
        {
            base.setDefaultMetadata();

            DefaultMetadata = JsonSerializer.Deserialize<ChatChunkMetadata>(JsonSerializer.Serialize(Metadata));
        }

        public void AppendMessage(ChatRole role, string message)
        {
            var meta = GetMeta<ChatChunkMetadata>();

            AddMetadata(nameof(ChatChunkMetadata.TEXT).ToLower(), (meta.TEXT + $"\n\n- ROLE: {role.ToString()} >> {message}").Trim());
            AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES), meta.TOTAL_MESSAGES + 1, resetDefault:true);
        }

        public List<ChatMessage> TextAsMessages()
        {
            if (string.IsNullOrEmpty(Text)) return [];

            var messages = new List<ChatMessage>();

            Text.Split("- ROLE:").ToList().ForEach(message =>
            {
                var split = message.Split(">>");

                var role = split[0].Trim().ToLower();

                messages.Add(new ChatMessage(role, split[1].Trim()));
            });

            return messages;
        }
    }
}
