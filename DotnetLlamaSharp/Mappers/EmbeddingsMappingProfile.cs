
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
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.DefaultMetadata.TEXT))
                .ForMember(dest => dest.Files, opt => opt.MapFrom(src => src.DefaultMetadata.FILES))
                .ForMember(dest => dest.TotalChunks, opt => opt.MapFrom(src => src.DefaultMetadata.CHUNKS))
                .ForMember(dest => dest.ChunkSize, opt => opt.MapFrom(src => src.DefaultMetadata.CHUNK_SIZE))
                .ForMember(dest => dest.ChunkOverlap, opt => opt.MapFrom(src => src.DefaultMetadata.CHUNK_OVERLAP))
                .ForMember(dest => dest.EmbeddingModel, opt => opt.MapFrom(src => src.DefaultMetadata.MODEL))
                .ForMember(dest => dest.EmbeddingDimensions, opt => opt.MapFrom(src => src.DefaultMetadata.DIMENSIONS));

            CreateMap<ChromaChunk, ChromaChunkDto>()
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Text))
                .ForMember(dest => dest.Embedding, opt => opt.MapFrom(src => src.Embedding))
                .ForMember(dest => dest.DocumentPageIds, opt => opt.MapFrom(src => src.DefaultMetadata.PAGES));

            CreateMap<CreateCollectionRequestDto, CreateCollectionRequest>();
            CreateMap<EmbedCollectionRequestDto, EmbedCollectionRequest>()
                .IncludeBase<CreateCollectionRequestDto, CreateCollectionRequest>();
        }
    }
}
