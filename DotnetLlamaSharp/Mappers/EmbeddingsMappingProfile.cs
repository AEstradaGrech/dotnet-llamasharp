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
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.DefaultMetadata.Text))
                .ForMember(dest => dest.Files, opt => opt.MapFrom(src => src.DefaultMetadata.Files))
                .ForMember(dest => dest.TotalChunks, opt => opt.MapFrom(src => src.DefaultMetadata.Chunks))
                .ForMember(dest => dest.EmbeddingModel, opt => opt.MapFrom(src => src.DefaultMetadata.Model))
                .ForMember(dest => dest.EmbeddingDimensions, opt => opt.MapFrom(src => src.DefaultMetadata.Dimensions));

            CreateMap<ChromaChunk, ChromaChunkDto>()
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Text))
                .ForMember(dest => dest.Embedding, opt => opt.MapFrom(src => src.Embedding))
                .ForMember(dest => dest.DocumentPageIds, opt => opt.MapFrom(src => src.DefaultMetadata.Pages));

            CreateMap<CreateCollectionRequestDto, CreateCollectionRequest>();
        }
    }
}
