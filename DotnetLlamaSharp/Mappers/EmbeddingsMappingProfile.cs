using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Embeddings;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Models.Request;
using DotnetLlamaSharp.Models.Response;

namespace DotnetLlamaSharp.Mappers
{
    public class EmbeddingsMappingProfile : Profile
    {
        public EmbeddingsMappingProfile()
        {
            CreateMap<ModelEmbeddings, EmbeddingsResponseDto>()
                .ForMember(dest => dest.Dimensions, opt => opt.MapFrom(src => src.GeneratedEmbeddings.Any() ? src.GeneratedEmbeddings.First().Dimensions : 0))
                .ForMember(dest => dest.Embeddings, opt => opt.MapFrom(src => src.GeneratedEmbeddings.Any() ? src.GeneratedEmbeddings.Select(x => x.Vector) : new List<ReadOnlyMemory<float>>()));

            CreateMap<ChromaCollection, ChromaCollectionDto>()
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Metadata.ContainsKey("description") ? src.Metadata["description"] : ""))
                .ForMember(dest => dest.TotalChunks, opt => opt.MapFrom(src => src.Metadata.ContainsKey("chunks") ? src.Metadata["chunks"] : 0))
                .ForMember(dest => dest.EmbeddingModel, opt => opt.MapFrom(src => src.Metadata.ContainsKey("model") ? src.Metadata["model"] : ""))
                .ForMember(dest => dest.ChunkDimensions, opt => opt.MapFrom(src => src.Metadata.ContainsKey("dimensions") ? src.Metadata["dimensions"] : ""));
            CreateMap<ChromaChunk, ChromaChunkDto>()
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Text))
                .ForMember(dest => dest.Embedding, opt => opt.MapFrom(src => src.Embedding))
                .ForMember(dest => dest.DocumentPageIds, opt => opt.MapFrom(src => src.Metadata.ContainsKey("pages") ? Convert.ToString(src.Metadata["pages"]).Split(",").ToList() : new List<string>()))
                .ForMember(dest => dest.PageSection, opt => opt.MapFrom(src => src.Metadata.ContainsKey("page_part") ? src.Metadata["page_part"] : null));

            CreateMap<CreateCollectionRequestDto, CreateCollectionRequest>();
        }
    }
}
