using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChunk : ChromaModel
    {
        public ChromaChunk() : base() 
        {
            Text = string.Empty;
        }
        public ChromaChunk(string id, Dictionary<string, object> metadata) : base(id, metadata) { Text = string.Empty; }
        public ChromaChunk(string id, string document, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id, metadata) { Text = document; Embedding = embedding; }

        public string Text { get; set; } //Document
        public ReadOnlyMemory<float> Embedding { get; set; }

        protected override void setDefaultMetadata()
        {
            DefaultMetadata = JsonSerializer.Deserialize<ChromaMetadata>(JsonSerializer.Serialize(Metadata));
        }

        public T As<T>() where T : ChromaChunk
            => Activator.CreateInstance(typeof(T), Id, Text, Embedding, Metadata) as T;
    }
}
