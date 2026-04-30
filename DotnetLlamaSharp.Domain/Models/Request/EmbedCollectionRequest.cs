using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class EmbedCollectionRequest : CreateCollectionRequest
    {
        public string FileName { get; set; }
        public int ChunkSize { get; set; } = 512;
        public int ChunkOverlap { get; set; } = 50;
        public int InitialSkip { get; set; } = 0;
        public int PageCutoff { get; set; } = 0;
        public int MinTextToChunk { get; set; } = 0;
        public Dictionary<string, object> Metadata { get; set; }
    }
}
