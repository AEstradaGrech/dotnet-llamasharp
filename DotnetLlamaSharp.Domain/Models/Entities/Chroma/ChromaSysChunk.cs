using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaSysChunk : ChromaChunk
    {
        public ChromaSysChunk() : base() { }
        public ChromaSysChunk(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public ChromaSysChunk(string id, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, metadata) { Embedding = embedding; }
        protected override void setDefaultMetadata()
        {
            base.setDefaultMetadata();

            DefaultMetadata = JsonSerializer.Deserialize<SysChunkMetadata>(JsonSerializer.Serialize(Metadata));
            Name = GetMeta<SysChunkMetadata>().NAME;
        }

        public string Name { get; set; }
        public string Tag { get; set; }
        
    }
}
