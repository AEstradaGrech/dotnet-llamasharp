using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Infrastructure.Settings
{
    public class ApiSettings
    {
        public string DefaultUserName { get; set; }
        public string DefaultModel { get; set; }
        public string DefaultEmbedder { get; set; }
        public int DefaultDimensions { get; set; } = 512;
        public float JsonOutputTopP { get; set; } = 0.1f;
        public int JsonOutputTopK { get; set; } = 10;
        public float JsonOutputTemp { get; set; } = 0.0f;
        public List<string> EmbeddingModels { get; set; }
        public List<string> JsonModels { get; set; }
        public int RagChatChunkSize { get; set; }
        public Dictionary<string, string> ServerUrls { get; set; }
        public int RagChatMemoryRetrievals { get; set; }
        public double RagChatMemoMinDistance { get; set; }
        public string EndpointByKey(string key) => ServerUrls.ContainsKey(key) ? ServerUrls[key] : "";
    }
}
