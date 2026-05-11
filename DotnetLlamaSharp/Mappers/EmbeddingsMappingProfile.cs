
using AutoMapper;
using Dotnet.Chroma.Repositories.Models;
using Dotnet.Chroma.Repositories.Models.Metadata;
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

            CreateMap<ChromaQuery, ChromaQueryResponseDto>();

            CreateMap<ChromaChunk, ChromaChunkDto>();

            CreateMap<ChromaQueryChunk, ChromaQueryChunkDto>()
                .ForMember(dest => dest.Embedding, opt => opt.Ignore())
                .ForMember(dest => dest.Document, opt => opt.MapFrom(src => src.DefaultMetadata.DOCUMENT_NAME));

            CreateMap<ChromaFileChunk, ChromaFileChunkDto>()
                .IncludeBase<ChromaChunk, ChromaChunkDto>()
                .ForMember(dest => dest.DocumentPageIds, opt => opt.MapFrom(src => src.GetMeta<FileChunkMetadata>().PAGES.Split(",").ToList()));
            
            CreateMap<ChromaSysChunk, ChromaSysChunkDto>()
                .IncludeBase<ChromaChunk, ChromaChunkDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.Name}-{src.GetMeta<SysChunkMetadata>().VERSION}"));

            CreateMap<ChromaChatChunk, ChromaChatChunkDto>()
                .ForMember(dest => dest.IsCurrent, opt => opt.MapFrom(src => src.GetMeta<ChatChunkMetadata>().CURRENT));

            CreateMap<ChromaChunksCollection<ChromaChunk>, ChromaChunksCollectionDto<ChromaChunkDto>>()
                .ForMember(dest => dest.EmbeddingModel, opt => opt.MapFrom(src => src.GetMeta<ChromaCollectionMetadata>().MODEL))
                .ForMember(dest => dest.EmbeddingDimensions, opt => opt.MapFrom(src => src.GetMeta<ChromaCollectionMetadata>().DIMENSIONS))
                .ForMember(dest => dest.TotalChunks, opt => opt.MapFrom(src => src.GetMeta<ChromaCollectionMetadata>().TOTAL_CHUNKS));

            CreateMap<ChromaChunksCollection<ChromaFileChunk>, ChromaChunksCollectionDto<ChromaFileChunkDto>>()
                .ForMember(dest => dest.EmbeddingModel, opt => opt.MapFrom(src => src.GetMeta<FileCollectionMetadata>().MODEL))
                .ForMember(dest => dest.EmbeddingDimensions, opt => opt.MapFrom(src => src.GetMeta<FileCollectionMetadata>().DIMENSIONS))
                .ForMember(dest => dest.TotalChunks, opt => opt.MapFrom(src => src.GetMeta<FileCollectionMetadata>().TOTAL_CHUNKS));

            CreateMap<ChromaChunksCollection<ChromaSysChunk>, ChromaChunksCollectionDto<ChromaSysChunkDto>>()
                .ForMember(dest => dest.EmbeddingModel, opt => opt.MapFrom(src => src.GetMeta<ChromaCollectionMetadata>().MODEL))
                .ForMember(dest => dest.EmbeddingDimensions, opt => opt.MapFrom(src => src.GetMeta<ChromaCollectionMetadata>().DIMENSIONS))
                .ForMember(dest => dest.TotalChunks, opt => opt.MapFrom(src => src.GetMeta<ChromaCollectionMetadata>().TOTAL_CHUNKS));

            CreateMap<ChromaChunksCollection<ChromaChatChunk>, ChromaChunksCollectionDto<ChromaChatChunkDto>>()
                .ForMember(dest => dest.EmbeddingModel, opt => opt.MapFrom(src => src.GetMeta<ChromaCollectionMetadata>().MODEL))
                .ForMember(dest => dest.EmbeddingDimensions, opt => opt.MapFrom(src => src.GetMeta<ChromaCollectionMetadata>().DIMENSIONS))
                .ForMember(dest => dest.TotalChunks, opt => opt.MapFrom(src => src.GetMeta<ChromaCollectionMetadata>().TOTAL_CHUNKS));

            CreateMap<SysChunksCollection, ChromaChunksCollectionDto<ChromaSysChunkDto>>()
                .IncludeBase<ChromaChunksCollection<ChromaSysChunk>, ChromaChunksCollectionDto<ChromaSysChunkDto>>();

            CreateMap<ChromaFilesCollection, ChromaFilesCollectionDto>()
                .IncludeBase<ChromaChunksCollection<ChromaFileChunk>, ChromaChunksCollectionDto<ChromaFileChunkDto>>()
                .ForMember(dest => dest.Chunks, opt => opt.MapFrom(src => src.Chunks))
                .ForMember(dest => dest.Files, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().FILES) ? new List<string>() : src.GetMeta<FileCollectionMetadata>().FILES.Split(",").ToList()))
                .ForMember(dest => dest.Pages, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().PAGES) ? 0 : int.Parse(src.GetMeta<FileCollectionMetadata>().PAGES)))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().TOPICS) ? new List<string>() : src.GetMeta<FileCollectionMetadata>().TOPICS.Split(",").ToList()))
                .ForMember(dest => dest.ChunkSizes, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().CHUNK_SIZES) ? new List<string>() : src.GetMeta<FileCollectionMetadata>().CHUNK_SIZES.Split(",").ToList()))
                .ForMember(dest => dest.ChunkOverlaps, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().CHUNK_OVERLAPS) ? new List<string>() : src.GetMeta<FileCollectionMetadata>().CHUNK_OVERLAPS.Split(",").ToList()))
                .ForMember(dest => dest.SkippedPages, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().SKIPPED_PAGES) ? new List<string>() : src.GetMeta<FileCollectionMetadata>().SKIPPED_PAGES.Split(",").ToList()))
                .ForMember(dest => dest.PageCutoffs, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.GetMeta<FileCollectionMetadata>().PAGE_CUTOFFS) ? new List<string>() : src.GetMeta<FileCollectionMetadata>().PAGE_CUTOFFS.Split(",").ToList()));

            CreateMap<ChromaChatsCollection, ChromaChatsCollectionDto>()
                .IncludeBase<ChromaChunksCollection<ChromaChatChunk>, ChromaChunksCollectionDto<ChromaChatChunkDto>>()
                .ForMember(dest => dest.CurrentSessionId, opt => opt.MapFrom(src => src.GetMeta<ChatCollectionMetadata>().CURRENT_SESSION_ID))
                .ForMember(dest => dest.SessionIds, opt => opt.MapFrom(src => src.GetMeta<ChatCollectionMetadata>().SESSION_IDS.Split(",").ToList()));

            CreateMap<ChromaChatsCollection, ChromaChatSessionDto>()
                .IncludeBase<ChromaChatsCollection, ChromaChatsCollectionDto>()
                .ForMember(dest => dest.CurrentSessionId, opt => opt.MapFrom(src => src.GetMeta<ChatCollectionMetadata>().CURRENT_SESSION_ID))
                .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => src.Chunks.First().GetMeta<ChatChunkMetadata>().SESSION_ID))
                .ForMember(dest => dest.TotalMessages, opt => opt.MapFrom(src => src.Chunks.First().GetMeta<ChatChunkMetadata>().TOTAL_MESSAGES))
                .ForMember(dest => dest.TotalChunks, opt => opt.MapFrom(src => src.Chunks.First().GetMeta<ChatChunkMetadata>().SESSION_CHUNKS));


            CreateMap<CreateCollectionRequestDto, CreateCollectionRequest>();

            CreateMap<EmbedCollectionRequestDto, EmbedCollectionRequest>()
                .IncludeBase<CreateCollectionRequestDto, CreateCollectionRequest>();
            
            CreateMap<CreateSysChunkRequestDto, ChromaSysChunk>()
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.Embedding, opt => opt.Ignore())
                .ForMember(dest => dest.DefaultMetadata, opt => opt.Ignore());

        }
    }
}
