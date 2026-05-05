using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaQueryChunk : ChromaChunk
    {
        public ChromaQueryChunk() : base() { }
        public ChromaQueryChunk(string id, double distance, string document, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata) : base(id,document, embedding, metadata) { Distance = distance; }
        public double Distance { get; set; }
    }
}
