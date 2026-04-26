namespace DotnetLlamaSharp.Models.Common.Documents
{
    public class DocumentDto
    {
        public string Name { get; set; }
        public string Source { get; set; }
        public int TotalPages { get; set; }
        public List<DocumentPageDto> Pages { get; set; }
    }
}
