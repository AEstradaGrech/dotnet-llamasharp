using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChatsCollection : ChromaChunksCollection<ChromaChatChunk> //Chunks son N last chunks de CurrentSessionId
    {
        public ChromaChatsCollection() : base() { }
        public ChromaChatsCollection(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public ChromaChatsCollection(string id, string name, string description, Dictionary<string, object> metadata): base(id, name, description, metadata) { }
        public ChromaChatsCollection(string id, string name, string description, Dictionary<string, object> metadata, List<ChromaChatChunk> chunks) : base(id, name, description, metadata, chunks) { }
        //Original display names. With whitespaces
        public string AgentName { get; set; }
        public string UserName { get; set; }
        public string CollectionName { get; set; }
        
        protected override void setDefaultMetadata()
        {
            DefaultMetadata = JsonSerializer.Deserialize<ChatCollectionMetadata>(JsonSerializer.Serialize(Metadata));
            AgentName = GetMeta<ChatCollectionMetadata>().AGENT_NAME;
            UserName = GetMeta<ChatCollectionMetadata>().USER_NAME;
            CollectionName = DefaultMetadata.DOCUMENT_NAME;
        }
    }
}
