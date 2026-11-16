using DotnetLlamaSharp.Models.Response.Chroma;

namespace DotnetLlamaSharp.Models.Response.Prompting
{
    public class RagPromptResponseDto : ChatPromptResponseDto
    {
        public string EmbeddingModel { get; set; }
        public List<ChromaQueryChunkDto> IncludedChunks { get; set; }
    }
}
