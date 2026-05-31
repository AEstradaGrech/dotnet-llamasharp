namespace DotnetLlamaSharp.Models.Request.Chroma
{
    public class PatchSysChunkRequestDto
    {
        public string NewText { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
