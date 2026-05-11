using Dotnet.Chroma.Repositories.Models;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting
{
    public class RagPrompt : ChatPrompt
    {
        public string EmbeddingModel { get; set; }
        public ReadOnlyMemory<float> InputEmbedding { get; set; }
        public List<ChromaQueryChunk> IncludedChunks { get; set; }
    }
}
