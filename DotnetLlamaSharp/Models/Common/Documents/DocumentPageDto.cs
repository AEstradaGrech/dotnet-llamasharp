namespace DotnetLlamaSharp.Models.Common.Documents
{
    public class DocumentPageDto
    {
        public DocumentPageDto() { }
       
        public int PageNumber { get; set; }
        public string Text { get; set; }
        public int Length { get; set; }
    }
}
