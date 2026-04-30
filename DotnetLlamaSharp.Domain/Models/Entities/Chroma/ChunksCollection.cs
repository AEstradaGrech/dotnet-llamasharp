using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChunksCollection : ChromaCollection
    {
        public ChunksCollection(string id, string name, Dictionary<string, object> metadata) : base(id, name, metadata)
        {
            Chunks = new List<ChromaChunk>();
        }
        public ChunksCollection(string id, string name, Dictionary<string, object> metadata, List<ChromaChunk> chunks) : base(id, name, metadata)
        {
            Chunks = chunks.OrderBy(x => int.Parse(x.Id)).ToList();
        }


        public List<ChromaChunk> Chunks { get; set; }
    }
}
