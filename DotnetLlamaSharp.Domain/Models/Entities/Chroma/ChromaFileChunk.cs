using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaFileChunk : ChromaChunk
    {
        public ChromaFileChunk() : base() { }
        public ChromaFileChunk(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public ChromaFileChunk(string id, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, embedding, metadata) {}

        protected override void setDefaultMetadata()
        {
            base.setDefaultMetadata();
            DefaultMetadata = JsonSerializer.Deserialize<FileChunkMetadata>(JsonSerializer.Serialize(Metadata));
        }
    }
}
