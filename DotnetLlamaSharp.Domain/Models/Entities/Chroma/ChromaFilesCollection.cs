using Dotnet.Chroma.Repositories.Models;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System.Text;
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

        public override string GetCollectionInfo()
            => new StringBuilder()
                .AppendLine(GetCollectionInfo<EChunkType>())
                .AppendLine($"- TOPICS: {GetMeta<FileCollectionMetadata>().TOPICS}")
                .AppendLine($"- FILES: {GetMeta<FileCollectionMetadata>().FILES}")
                .ToString()
                .Trim();
        
    }
}
