using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaFilesCollection : ChromaChunksCollection<ChromaFileChunk>
    {
        public ChromaFilesCollection() : base() { }
        public ChromaFilesCollection(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public ChromaFilesCollection(string id, string name, string description, Dictionary<string, object> metadata) : base(id, name, description, metadata) {}

        public ChromaFilesCollection(string id, string name, string description, Dictionary<string, object> metadata, List<ChromaFileChunk> chunks) : base(id, name, description, metadata, chunks) { }

        protected override void setDefaultMetadata()
        {
            base.setDefaultMetadata();

            DefaultMetadata = JsonSerializer.Deserialize<FileCollectionMetadata>(JsonSerializer.Serialize(Metadata));
        }
    }
}
