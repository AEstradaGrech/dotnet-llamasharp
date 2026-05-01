using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaFileChunk : ChromaChunk
    {
        public ChromaFileChunk() { }
        public ChromaFileChunk(string id, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, embedding, metadata) {}

        public new FileChunkMetadata DefaultMetadata { get; set; }
        protected override void setDefaultMetadata()
        {
            DefaultMetadata = JsonSerializer.Deserialize<FileChunkMetadata>(JsonSerializer.Serialize(Metadata));
        }
    }
}
