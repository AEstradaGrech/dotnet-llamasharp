namespace DotnetLlamaSharp.Models.Response
{
    public class RagPromptResponseDto : ChatPromptResponseDto
    {
        public string EmbeddingModel { get; set; }
        public List<ChromaQueryChunkDto> IncludedChunks { get; set; }
    }
}
