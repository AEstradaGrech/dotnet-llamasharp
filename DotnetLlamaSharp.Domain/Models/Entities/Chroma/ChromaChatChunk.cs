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
        public ChromaChatChunk(string id, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, embedding, metadata) { }
        public ChromaChatChunk(string id, string text, Dictionary<string, object> metadata) : base(id, text, metadata) { }
        public new ChatChunkMetadata DefaultMetadata { get; set; }

        protected override void setDefaultMetadata()
        {
            DefaultMetadata = JsonSerializer.Deserialize<ChatChunkMetadata>(JsonSerializer.Serialize(Metadata));
        }

        public void AppendMessage(ChatRole role, string message)
        {
            Text += $"\n#MSG#\n- ROLE: {role.ToString()} >> {message}";
            Text = Text.Trim();
            
            var currentMessages = JsonSerializer.Deserialize<int>((JsonValue)Metadata[nameof(ChatChunkMetadata.TOTAL_MESSAGES)]);

            AddMetadata(nameof(ChatChunkMetadata.TOTAL_MESSAGES), currentMessages);
        }

        public List<ChatMessage> TextAsMessages()
        {
            if (string.IsNullOrEmpty(Text)) return [];

            var messages = new List<ChatMessage>();

            Text.Split("#MSG#").ToList().ForEach(line =>
            {
                var split = line.Split(">>");

                var role = split[0].Split("- ROLE:")[1].Trim().ToLower();

                messages.Add(new ChatMessage(role, split[1].Trim()));
            });

            return messages;
        }
    }
}
