using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaCollection : ChromaModel
    {
        public ChromaCollection(string id, string name, Dictionary<string, object> metadata) 
        {
            Id = id;
            Name = name;
            Metadata = metadata;
            DefaultMetadata = JsonSerializer.Deserialize<CollectionMetadata>(JsonSerializer.Serialize(Metadata));
        }

        public string Name { get; set; }
        public CollectionMetadata DefaultMetadata { get; set; }
    }
}
