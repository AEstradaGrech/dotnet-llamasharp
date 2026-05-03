
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

            CreateMap<ChromaChunk, ChromaChunkDto>();

            CreateMap<ChromaFileChunk, ChromaFileChunkDto>()
                .IncludeBase<ChromaChunk, ChromaChunkDto>()
                .ForMember(dest => dest.DocumentPageIds, opt => opt.MapFrom(src => src.GetMeta<FileChunkMetadata>().PAGES.Split(",").ToList()));

            CreateMap<ChromaChunksCollection<ChromaChunk>, ChromaChunksCollectionDto<ChromaChunkDto>>()
                .ForMember(dest => dest.EmbeddingModel, opt => opt.MapFrom(src => src.GetMeta<ChromaCollectionMetadata>().MODEL))
                .ForMember(dest => dest.EmbeddingDimensions, opt => opt.MapFrom(src => src.GetMeta<ChromaCollectionMetadata>().DIMENSIONS))
                .ForMember(dest => dest.TotalChunks, opt => opt.MapFrom(src => src.GetMeta<ChromaCollectionMetadata>().TOTAL_CHUNKS));

            CreateMap<ChromaChunksCollection<ChromaFileChunk>, ChromaChunksCollectionDto<ChromaFileChunkDto>>()
                .ForMember(dest => dest.EmbeddingModel, opt => opt.MapFrom(src => src.GetMeta<FileCollectionMetadata>().MODEL))
                .ForMember(dest => dest.EmbeddingDimensions, opt => opt.MapFrom(src => src.GetMeta<FileCollectionMetadata>().DIMENSIONS))
                .ForMember(dest => dest.TotalChunks, opt => opt.MapFrom(src => src.GetMeta<FileCollectionMetadata>().TOTAL_CHUNKS));

            CreateMap<ChromaFilesCollection, ChromaFilesCollectionDto>()
                .IncludeBase<ChromaChunksCollection<ChromaFileChunk>, ChromaChunksCollectionDto<ChromaFileChunkDto>>()
                .ForMember(dest => dest.Chunks, opt => opt.MapFrom(src => src.Chunks))
                .ForMember(dest => dest.Files, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().FILES) ? new List<string>() : src.GetMeta<FileCollectionMetadata>().FILES.Split(",").ToList()))
                .ForMember(dest => dest.Pages, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().PAGES) ? 0 : int.Parse(src.GetMeta<FileCollectionMetadata>().PAGES)))
                .ForMember(dest => dest.ChunkSizes, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().CHUNK_SIZES) ? new List<string>() : src.GetMeta<FileCollectionMetadata>().CHUNK_SIZES.Split(",").ToList()))
                .ForMember(dest => dest.ChunkOverlaps, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().CHUNK_OVERLAPS) ? new List<string>() : src.GetMeta<FileCollectionMetadata>().CHUNK_OVERLAPS.Split(",").ToList()))
                .ForMember(dest => dest.SkippedPages, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().SKIPPED_PAGES) ? new List<string>() : src.GetMeta<FileCollectionMetadata>().SKIPPED_PAGES.Split(",").ToList()))
                .ForMember(dest => dest.PageCutoffs, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().PAGE_CUTOFFS) ? new List<string>() : src.GetMeta<FileCollectionMetadata>().PAGE_CUTOFFS.Split(",").ToList()));

            

            CreateMap<CreateCollectionRequestDto, CreateCollectionRequest>();

            CreateMap<EmbedCollectionRequestDto, EmbedCollectionRequest>()
                .IncludeBase<CreateCollectionRequestDto, CreateCollectionRequest>();

            CreateMap<ChromaQueryChunk, ChromaQueryChunkDto>()
                .ForMember(dest => dest.Embedding, opt => opt.Ignore())
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.DefaultMetadata.TEXT))
                .ForMember(dest => dest.Document, opt => opt.MapFrom(src => src.DefaultMetadata.DOCUMENT));

            CreateMap<ChromaQuery, ChromaQueryResponseDto>();

            CreateMap<ChromaQueryChunk, ChromaChatChunk>()
                .ForMember(dest => dest.Embedding, opt => opt.Ignore());
        }
    }
}
