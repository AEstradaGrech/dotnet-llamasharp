namespace DotnetLlamaSharp.Models.Request
{
    public class PatchSysChunkRequestDto
    {
        public string NewText { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
