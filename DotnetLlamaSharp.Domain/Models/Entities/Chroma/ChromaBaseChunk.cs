using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaBaseChunk : ChromaModel
    {
        public ChromaBaseChunk() { }
        public ChromaBaseChunk(string id, Dictionary<string, object> metadata) : base(id)
        {
            Metadata = metadata;
            DefaultMetadata = JsonSerializer.Deserialize<ChunkMetadata>(JsonSerializer.Serialize(Metadata));
        }

        public ChunkMetadata DefaultMetadata { get; set; }
    }
}
