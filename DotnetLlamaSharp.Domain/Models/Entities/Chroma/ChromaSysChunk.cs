using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaSysChunk : ChromaChunk
    {
        public ChromaSysChunk() : base() { Type = EChunkType.SYSTEM; }
        public ChromaSysChunk(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public ChromaSysChunk(string id, string document, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, document, embedding, metadata) { }
        protected override void setDefaultMetadata()
        {
            base.setDefaultMetadata();

            DefaultMetadata = JsonSerializer.Deserialize<SysChunkMetadata>(JsonSerializer.Serialize(Metadata));
            
            Name = GetMeta<SysChunkMetadata>().DOCUMENT_NAME;
            Tag = GetMeta<SysChunkMetadata>().TAG;
        }

        public string Name { get; set; }
        public string Tag { get; set; }
        
    }
}
