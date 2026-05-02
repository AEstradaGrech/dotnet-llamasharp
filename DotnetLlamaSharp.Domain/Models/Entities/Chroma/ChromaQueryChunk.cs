using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaQueryChunk : ChromaBaseChunk
    {
        public ChromaQueryChunk() : base() { }
        public ChromaQueryChunk(string id, double distance, Dictionary<string, object> metadata) : base(id, metadata)
        {
            Distance = distance;
        }
        public double Distance { get; set; }
    }
}
