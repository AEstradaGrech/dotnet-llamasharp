using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Primitives.DocumentLoader;
using DotnetLlamaSharp.Models.Common.Documents;

namespace DotnetLlamaSharp.Mappers
{
    public class DocumentsMappingProfile : Profile
    {
        public DocumentsMappingProfile() 
        {
            CreateMap<Document, DocumentDto>()
                .ReverseMap();
            CreateMap<DocumentPage, DocumentPageDto>()
                .ReverseMap();
        }
    }
}
