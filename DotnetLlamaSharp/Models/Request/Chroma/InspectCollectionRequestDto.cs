namespace DotnetLlamaSharp.Models.Request.Chroma
{
    public class InspectCollectionRequestDto
    {
        public string Name { get; set; }
        public int StartIndex { get; set; }
        public int SamplesNumber { get; set; }
        public bool IncludeEmbeddings { get; set; }
    }
}
