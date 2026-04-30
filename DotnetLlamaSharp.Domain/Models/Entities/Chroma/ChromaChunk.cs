using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChunk : ChromaModel
    {
        public ChromaChunk() { }
        public ChromaChunk(string id, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata)
        {
            Id = id;
            Metadata = metadata;
            Embedding = embedding;
            DefaultMetadata = JsonSerializer.Deserialize<ChunkMetadata>(JsonSerializer.Serialize(Metadata));
            Text = DefaultMetadata.TEXT; //TODO remove text from meta | main model
        }
        public string Text { get; set; }
        public ReadOnlyMemory<float> Embedding { get; set; }
        // 'mandatory' metadatas
        public ChunkMetadata DefaultMetadata { get; set; }

    }
}
