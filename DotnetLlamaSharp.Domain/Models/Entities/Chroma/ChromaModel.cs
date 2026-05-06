

using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public abstract class ChromaModel
    {
        public ChromaModel() 
        {
            Type = EChunkType.DEFAULT;

            Metadata = new Dictionary<string, object>() { [nameof(ChromaMetadata.TYPE).ToLower()] = (int)Type };

            setDefaultMetadata(); 
        }
        public ChromaModel(string id) : this() { Id = id; }
        public ChromaModel(string id, Dictionary<string, object> metadata) : this(id) 
        {
            Metadata = metadata; 
            
            setDefaultMetadata(); 
        }

        public string Id { get; set; }
        public EChunkType Type { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public ChromaMetadata DefaultMetadata { get; set; }

        public EChunkType ChunkType => DefaultMetadata != null ? (EChunkType)DefaultMetadata.TYPE : EChunkType.DEFAULT; 

        public void UpdateType(EChunkType type)
        {
            Type = type;
            
            AddMetadata(nameof(ChromaMetadata.TYPE).ToLower(), (int)Type);
            
            if (DefaultMetadata != null)
                DefaultMetadata.TYPE = (int)Type;
        }
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
       
        public T GetMeta<T>() where T : ChromaMetadata
            => (T)DefaultMetadata;
    }
}
