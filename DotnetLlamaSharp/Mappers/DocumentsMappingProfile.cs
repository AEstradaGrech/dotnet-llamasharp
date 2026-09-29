using AutoMapper;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.DocumentLoader;
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
