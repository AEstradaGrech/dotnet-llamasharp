using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChunk
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public ReadOnlyMemory<float> Embedding { get; set; }
        public Dictionary<string,object> Metadata { get; set; }
    }
}
