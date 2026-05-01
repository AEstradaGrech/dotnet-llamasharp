
using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
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
                .ForMember(dest => dest.Files, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.DefaultMetadata.FILES) ? new List<string>() : src.DefaultMetadata.FILES.Split(",").ToList()))
                .ForMember(dest => dest.Pages, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.DefaultMetadata.PAGES) ? 0 : int.Parse(src.DefaultMetadata.PAGES)))
                .ForMember(dest => dest.TotalChunks, opt => opt.MapFrom(src => src.DefaultMetadata.CHUNKS))
                .ForMember(dest => dest.ChunkSizes, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.DefaultMetadata.CHUNK_SIZES) ? new List<string>() : src.DefaultMetadata.CHUNK_SIZES.Split(",").ToList()))
                .ForMember(dest => dest.ChunkOverlaps, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.DefaultMetadata.CHUNK_OVERLAPS) ? new List<string>() : src.DefaultMetadata.CHUNK_OVERLAPS.Split(",").ToList()))
                .ForMember(dest => dest.SkippedPages, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.DefaultMetadata.SKIPPED_PAGES) ? new List<string>() : src.DefaultMetadata.SKIPPED_PAGES.Split(",").ToList()))
                .ForMember(dest => dest.PageCutoffs, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.DefaultMetadata.PAGE_CUTOFFS) ? new List<string>() : src.DefaultMetadata.PAGE_CUTOFFS.Split(",").ToList()))
                .ForMember(dest => dest.EmbeddingModel, opt => opt.MapFrom(src => src.DefaultMetadata.MODEL))
                .ForMember(dest => dest.EmbeddingDimensions, opt => opt.MapFrom(src => src.DefaultMetadata.DIMENSIONS));
            
            CreateMap<ChunksCollection, ChunksCollectionDto>()
                .IncludeBase<ChromaCollection, ChromaCollectionDto>();

            CreateMap<ChromaChunk, ChromaChunkDto>()
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Text))
                .ForMember(dest => dest.Embedding, opt => opt.MapFrom(src => src.Embedding))
                .ForMember(dest => dest.DocumentPageIds, opt => opt.MapFrom(src => src.DefaultMetadata.PAGES.Split(",").ToList()));

            CreateMap<CreateCollectionRequestDto, CreateCollectionRequest>();
            
            CreateMap<EmbedCollectionRequestDto, EmbedCollectionRequest>()
                .IncludeBase<CreateCollectionRequestDto, CreateCollectionRequest>();

            CreateMap<ChromaQueryChunk, ChromaQueryChunkDto>()
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.DefaultMetadata.TEXT))
                .ForMember(dest => dest.Pages, opt => opt.MapFrom(src => src.DefaultMetadata.PAGES.Split(",").ToList()))
                .ForMember(dest => dest.Document, opt => opt.MapFrom(src => src.DefaultMetadata.DOCUMENT));

            CreateMap<ChromaQuery, ChromaQueryResponseDto>();
        }
    }
}
