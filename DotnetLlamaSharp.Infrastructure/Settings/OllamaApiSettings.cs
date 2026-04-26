using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Infrastructure.Settings
{
    public class OllamaApiSettings
    {
        public string ServerUrl { get; set; }
        public string DefaultModel { get; set; }
        public string DefaultEmbedder { get; set; }
        public List<string> EmbeddingModels { get; set; }
    }
}
