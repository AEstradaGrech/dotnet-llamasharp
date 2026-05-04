using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChatCollection : ChromaChunksCollection<ChromaChatChunk> //Chunks son N last chunks de CurrentSessionId
    {
        public ChromaChatCollection() : base() { }
        public ChromaChatCollection(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public ChromaChatCollection(string id, string name, Dictionary<string, object> metadata): base(id, name, metadata) { }

        //Original display names. With whitespaces
        public string AgentName { get; set; }
        public string UserName { get; set; }
        public string CollectionName { get; set; }
        
        protected override void setDefaultMetadata()
        {
            DefaultMetadata = JsonSerializer.Deserialize<ChatCollectionMetadata>(JsonSerializer.Serialize(Metadata));
            Description = DefaultMetadata.TEXT;
            AgentName = GetMeta<ChatCollectionMetadata>().AGENT_NAME;
            UserName = GetMeta<ChatCollectionMetadata>().USER_NAME;
            CollectionName = DefaultMetadata.DOCUMENT;
        }
    }
}
