using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChunk : ChromaBaseChunk
    {
        public ChromaChunk() : base() {  }
        public ChromaChunk(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public ChromaChunk(string id, string document, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, metadata) { Text = document; Embedding = embedding; }

        public string Text { get; set; } //Document
        public ReadOnlyMemory<float> Embedding { get; set; }

        protected override void setDefaultMetadata()
        {
            base.setDefaultMetadata();

            //Text = DefaultMetadata.DOCUMENT; //TODO remove text from meta | main model
        }
    }
}
