namespace DotnetLlamaSharp.Models.Response.Chroma
{
    public class ChromaQueryResponseDto
    {
        public string Query { get; set; }
        public string Collection { get; set; }
        public string EmbeddingModel { get; set; }
        public int Dimensions { get; set; }
        public List<ChromaQueryChunkDto> Chunks { get; set; }
    }
}
