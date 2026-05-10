namespace DotnetLlamaSharp.Models.Request
{
    public class SmartQueryRequestDto : SimplePromptRequestDto
    {
        public bool WithChatCollections { get; set; }
        public bool WithQueryAugmentation { get; set; }
        public bool WithQueryExpansion { get; set; }
        public bool WithLangSearch { get; set; }
    }
}
