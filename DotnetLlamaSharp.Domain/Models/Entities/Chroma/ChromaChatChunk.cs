using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChatChunk : ChromaChunk
    {
        public ChromaChatChunk(string id, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, embedding, metadata) { }

        public new ChatChunkMetadata DefaultMetadata { get; set; }
        protected override void setDefaultMetadata()
        {
            DefaultMetadata = JsonSerializer.Deserialize<ChatChunkMetadata>(JsonSerializer.Serialize(Metadata));
        }
    }
}
