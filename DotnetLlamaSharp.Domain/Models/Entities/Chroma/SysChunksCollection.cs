using Dotnet.Chroma.Repositories.Models;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class SysChunksCollection : ChromaChunksCollection<ChromaSysChunk>
    {
        public SysChunksCollection() : base() { }
        public SysChunksCollection(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public SysChunksCollection(string id, string name, string description, Dictionary<string, object> metadata) : base(id, name, description, metadata) { }
        public SysChunksCollection(string id, string name, string description, Dictionary<string, object> metadata, List<ChromaSysChunk> chunks) : base(id, name, description, metadata, chunks) { }
    }
}
