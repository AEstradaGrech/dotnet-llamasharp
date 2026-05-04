using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChunk : ChromaBaseChunk
    {
        public ChromaChunk() : base() {  }
        public ChromaChunk(string id, Dictionary<string, object> metadata) : base(id, metadata) { }
        public ChromaChunk(string id, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, metadata) { Embedding = embedding; }
        public ChromaChunk(string id, string text, Dictionary<string, object> metadata) : base(id, metadata) { Text = text; }

        public string Text { get; set; }
        public ReadOnlyMemory<float> Embedding { get; set; }

        protected override void setDefaultMetadata()
        {
            base.setDefaultMetadata();

            Text = DefaultMetadata.TEXT; //TODO remove text from meta | main model
        }
    }
}
