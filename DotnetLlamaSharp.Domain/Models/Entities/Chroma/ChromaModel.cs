

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public abstract class ChromaModel
    {
        public ChromaModel() { }
        public ChromaModel(string id)
        {
            Id = id;
        }

        public string Id { get; set; }

        // all metadata for write, extra-metas for read
        public Dictionary<string, object> Metadata { get; set; }

        public void AddMetadata(string key, object value)
        {
            if (Metadata.ContainsKey(key))
                Metadata[key] = value;

            else Metadata.Add(key, value);
        }

        public bool HasMetadata(string key) => Metadata != null && Metadata.ContainsKey(key);
    }
}
