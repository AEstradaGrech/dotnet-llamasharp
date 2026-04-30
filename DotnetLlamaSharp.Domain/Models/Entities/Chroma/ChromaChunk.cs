using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChunk : ChromaBaseChunk
    {
        public ChromaChunk() { }
        public ChromaChunk(string id, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, metadata)
        {
            Embedding = embedding;
            Text = DefaultMetadata.TEXT; //TODO remove text from meta | main model
        }
        public string Text { get; set; }
        public ReadOnlyMemory<float> Embedding { get; set; }
    }
}
