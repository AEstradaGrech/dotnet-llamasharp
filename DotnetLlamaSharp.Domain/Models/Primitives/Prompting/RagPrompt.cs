using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting
{
    public class RagPrompt : ChatPrompt
    {
        public string EmbeddingModel { get; set; }
        public ReadOnlyMemory<float> InputEmbedding { get; set; }
        public List<ChromaQueryChunk> IncludedChunks { get; set; }
    }
}
