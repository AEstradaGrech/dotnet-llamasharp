using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChunksCollection<T> : ChromaBaseChunk where T : ChromaChunk
    {
        public ChromaChunksCollection() : base() {}
        public ChromaChunksCollection(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public ChromaChunksCollection(string id, string name, string description, Dictionary<string, object> metadata) : base(id, metadata)
        {
            Name = name;
            Description = description;
            Chunks = new List<T>();
        }
        public ChromaChunksCollection(string id, string name, string description, Dictionary<string, object> metadata, List<T> chunks) : this(id, name, description, metadata)
        {
            Chunks = chunks.OrderBy(x => int.Parse(x.Id)).ToList();
        }
        
        public string Name { get; set; }
        public string Description { get; set; }
        public List<T> Chunks { get; set; }

        protected override void setDefaultMetadata()
        {
            DefaultMetadata = JsonSerializer.Deserialize<ChromaCollectionMetadata>(JsonSerializer.Serialize(Metadata));
        }
    }
}
