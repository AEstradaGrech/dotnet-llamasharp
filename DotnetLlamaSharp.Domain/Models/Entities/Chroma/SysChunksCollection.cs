using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class SysChunksCollection : ChromaChunksCollection<ChromaSysChunk>
    {
        public SysChunksCollection() : base() { }
        public SysChunksCollection(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public SysChunksCollection(string id, string name, Dictionary<string, object> metadata) : base(id, name, metadata) { }
        public ReadOnlyMemory<float> CollectionEmbedding { get; set; }
        protected override void setDefaultMetadata()
        {
            base.setDefaultMetadata();
            DefaultMetadata = JsonSerializer.Deserialize<SysCollectionMetadata>(JsonSerializer.Serialize(Metadata));
            CollectionEmbedding = GetMeta<SysCollectionMetadata>().COL_EMBEDDING;
        }
    }
}
