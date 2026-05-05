using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaFileChunk : ChromaChunk
    {
        public ChromaFileChunk() : base() { }
        public ChromaFileChunk(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public ChromaFileChunk(string id, string text, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, text, embedding, metadata) {}

        protected override void setDefaultMetadata()
        {
            base.setDefaultMetadata();
            DefaultMetadata = JsonSerializer.Deserialize<FileChunkMetadata>(JsonSerializer.Serialize(Metadata));
        }
    }
}
