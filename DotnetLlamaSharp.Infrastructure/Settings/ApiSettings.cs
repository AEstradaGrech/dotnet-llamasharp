using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Infrastructure.Settings
{
    public class ApiSettings
    {
        public string DefaultModel { get; set; }
        public string DefaultEmbedder { get; set; }
        public List<string> EmbeddingModels { get; set; }

        public Dictionary<string, string> ServerUrls { get; set; }

        public string ServerByKey(string key) => ServerUrls.ContainsKey(key) ? ServerUrls[key] : "";
    }
}
