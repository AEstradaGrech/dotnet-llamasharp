

using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public abstract class ChromaModel
    {
        public ChromaModel() { Metadata = new Dictionary<string, object>(); }
        public ChromaModel(string id) : this() { Id = id; }
        public string Id { get; set; }

        // all metadata for write, extra-metas for read
        public Dictionary<string, object> Metadata { get; set; }

        public void AddMetadata(string key, object value, bool resetDefault = false, bool isOverride = true)
        {
            if (Metadata.ContainsKey(key))
            {
                if (isOverride)
                    Metadata[key] = value;
            }
            else Metadata.Add(key, value);

            if (resetDefault)
                setDefaultMetadata();
        }

        public bool TryRemoveMetadata(string key, bool updateDefault = false)
        {
            if (!HasMetadata(key)) return false;

            if(Metadata.Remove(key))
            {
                if (updateDefault)
                    setDefaultMetadata();

                return true;
            }

            return false;
        }

        public void CloneMetadata(Dictionary<string, object> metadata, bool resetDefault = false)
        {
            var clone = new Dictionary<string, object>();

            foreach (var key in metadata.Keys)
                clone.Add(key, metadata[key]);

            Metadata = clone;

            if (resetDefault)
                setDefaultMetadata();
        }
        public bool HasMetadata(string key) => Metadata != null && Metadata.ContainsKey(key);

        protected abstract void setDefaultMetadata();
    }
}
