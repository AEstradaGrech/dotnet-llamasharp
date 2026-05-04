using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaFilesCollection : ChromaChunksCollection<ChromaFileChunk>
    {
        public ChromaFilesCollection() : base() { }
        public ChromaFilesCollection(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public ChromaFilesCollection(string id, string name, Dictionary<string, object> metadata) : base(id, name, metadata) {}

        public ChromaFilesCollection(string id, string name, Dictionary<string, object> metadata, List<ChromaFileChunk> chunks) : base(id, name, metadata, chunks) { }

        protected override void setDefaultMetadata()
        {
            DefaultMetadata = JsonSerializer.Deserialize<FileCollectionMetadata>(JsonSerializer.Serialize(Metadata));
            Description = DefaultMetadata.TEXT;
        }
    }
}
