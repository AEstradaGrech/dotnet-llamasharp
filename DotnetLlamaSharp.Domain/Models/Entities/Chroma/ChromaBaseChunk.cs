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

            setDefaultMetadata();
        }

        public ChromaMetadata DefaultMetadata { get; set; }

        protected override void setDefaultMetadata()
        {
            DefaultMetadata = JsonSerializer.Deserialize<ChromaMetadata>(JsonSerializer.Serialize(Metadata));
        }
    }
}
