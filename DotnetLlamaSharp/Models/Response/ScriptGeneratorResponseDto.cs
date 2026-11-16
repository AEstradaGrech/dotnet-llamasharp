
namespace DotnetLlamaSharp.Models.Response
{
    public class ScriptGeneratorResponseDto
    {
        public string Script { get; set; } = string.Empty;
        public string Shell { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public bool ChangesSystem { get; set; }
        public bool RequiresAdmin { get; set; }
        public string? Undo { get; set; }
        public List<string> Arguments { get; set; } = [];
        public string? OutputFolder { get; set; }
        public string? ScriptPath { get; set; }
    }
}
