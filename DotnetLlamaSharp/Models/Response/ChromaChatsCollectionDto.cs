namespace DotnetLlamaSharp.Models.Response
{
    public class ChromaChatsCollectionDto : ChromaChunksCollectionDto<ChromaChatChunkDto>
    {
        public string AgentName { get; set; }
        public string UserName { get; set; }
        public string CollectionName { get; set; }
        public string CurrentSessionId { get; set; }
        public List<string> SessionIds { get; set; }
    }
}
