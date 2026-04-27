using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class CreateCollectionRequest
    {
        public string Name { get; set; }
        public string EmbeddingModel { get; set; }
        public string EmbeddingDimensions { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public int ChunkSize { get; set; } = 512;
        public Dictionary<string, string> MetadataTags { get; set; }
    }
}
