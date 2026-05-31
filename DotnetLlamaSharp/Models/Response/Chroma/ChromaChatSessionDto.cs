namespace DotnetLlamaSharp.Models.Response.Chroma
{
    public class ChromaChatSessionDto : ChromaChatsCollectionDto
    {
        public string AgentName { get; set; }
        public string UserName { get; set; }
        public string CollectionName { get; set; }

        public string SessionId { get; set; }
        public int TotalMessages { get; set; }
        public int TotalChunks { get; set; }
    }
}
