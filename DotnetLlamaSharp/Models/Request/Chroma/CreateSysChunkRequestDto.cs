namespace DotnetLlamaSharp.Models.Request.Chroma
{
    public class CreateSysChunkRequestDto
    {
        public string Name { get; set; }
        public string Message { get; set; }
        public string? Tag { get; set; } = string.Empty;
        public string? Version { get; set; } = null;
    }
}
