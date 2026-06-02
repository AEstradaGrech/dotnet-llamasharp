using AutoMapper;
using Dotnet.LangSearch.SDK.Models.Request;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Models.Request.Prompting;
using DotnetLlamaSharp.Models.Common;
using DotnetLlamaSharp.Models.Request;
using DotnetLlamaSharp.Models.Request.LangSearch;
using DotnetLlamaSharp.Models.Response;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Mappers
{
    public class PromptsMappingProfile : Profile
    {
        public PromptsMappingProfile()
        {
            //DTO to Domain settings
            CreateMap<PromptSettingsDto, PromptSettings>();
            CreateMap<PromptSettingsDto, CommandSettings>();

            CreateMap<PromptSettings, PromptSettingsDto>()
                .ForMember(dest => dest.UseDefaultCommandMessage, opt => opt.Ignore())
                .ForMember(dest => dest.ValidatorModel, opt => opt.Ignore())
                .ForMember(dest => dest.CommandValidations, opt => opt.Ignore())
                .ForMember(dest => dest.ValidationType, opt => opt.Ignore());

            CreateMap<CommandSettings, PromptSettingsDto>();

            // Domain to Domain settings models

            CreateMap<PromptSettings, CommandSettings>()
               .ForMember(dest => dest.UseDefaultCommandMessage, opt => opt.Ignore())
               .ForMember(dest => dest.CommandValidations, opt => opt.Ignore())
               .ForMember(dest => dest.ValidationType, opt => opt.Ignore())
               .ForMember(dest => dest.ValidatorModel, opt => opt.Ignore());
            CreateMap<CommandSettings, PromptSettings>();
            CreateMap<CommandSettings, CommandSettings>();

            CreateMap<ChatPrompt, ChatPromptResponseDto>();
            CreateMap<RagPrompt, RagPromptResponseDto>();

            CreateMap<ChatMessage, ChatMessageDto>()
                .ReverseMap();
            //DTO to Domain
            CreateMap<SimplePromptRequestDto, SimplePromptRequest>()
                .ForMember(dest => dest.Settings, opt => opt.MapFrom(src => src.Settings));

            CreateMap<ChatPromptRequestDto, ChatPromptRequest>()
                .IncludeBase<SimplePromptRequestDto, SimplePromptRequest>();

            CreateMap<ChatPromptRequestDto, CommandChatRequest>()
                .IncludeBase<SimplePromptRequestDto, SimpleCommandRequest>();

            CreateMap<RagPromptRequestDto, RagCommandRequest>()
                .IncludeBase<SimplePromptRequestDto, SimpleCommandRequest>();
           
            CreateMap<RagChatRequestDto, RagChatCommandRequest>()
                .IncludeBase<RagPromptRequestDto, RagCommandRequest>();
            
            CreateMap<SmartQueryRequestDto, SimpleSmartQueryRequest>()
                .IncludeBase<SimplePromptRequestDto, SimpleCommandRequest>();

            CreateMap<RagChatRequestDto, RagChatCommandRequest>()
               .IncludeBase<RagPromptRequestDto, RagCommandRequest>();

            CreateMap<SmartQueryRequestDto, SimpleSmartQueryRequest>()
                .IncludeBase<SimplePromptRequestDto, SimpleCommandRequest>();
            
           
            // Domain to Domain models
            //CreateMap<SimplePromptRequest, RagCommandRequest>()
            //    .ForMember(dest => dest.CollectionRetrievals, opt => opt.Ignore())
            //    .ForMember(dest => dest.QueryCollections, opt => opt.Ignore())
            //    .ForMember(dest => dest.EmbeddingFilters, opt => opt.Ignore())
            //    .ForMember(dest => dest.MinDistance, opt => opt.Ignore());
            //CreateMap<RagCommandRequest, SimplePromptRequest>();


            //CreateMap<SmartQueryRequest, SimplePromptRequest>()
            //    .ForMember(dest => dest.Settings, opt => opt.MapFrom(opt => opt.Settings));
            
            //CreateMap<SmartQueryRequestDEP, SimpleCommandRequest>()
            //    .ForMember(dest => dest.Settings, opt => opt.MapFrom(opt => opt.Settings));

            CreateMap<SimpleSmartQueryRequest, SmartRagSettings>()
                .ForMember(dest => dest.MaxExamples, opt => opt.MapFrom(src => src.MaxFewShotExamples))
                .ForMember(dest => dest.WithQueryAugmentation, opt => opt.Ignore())
                .ForMember(dest => dest.WithRagExpansion, opt => opt.Ignore());

            CreateMap<SimpleCommandRequest, CommandChatRequest>()
                .ForMember(dest => dest.ChatHistory, opt => opt.Ignore());
            CreateMap<CommandChatRequest, SimpleCommandRequest>();

            CreateMap<SimpleCommandRequest, RagCommandRequest>()
                .ForMember(dest => dest.QueryCollections, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddingFilters, opt => opt.Ignore())
                .ForMember(dest => dest.MaxDistance, opt => opt.Ignore())
                .ForMember(dest => dest.CollectionRetrievals, opt => opt.Ignore());
            CreateMap<RagCommandRequest, SimpleCommandRequest>();

            CreateMap<CommandChatRequest, RagCommandRequest>()
                .ForMember(dest => dest.QueryCollections, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddingFilters, opt => opt.Ignore())
                .ForMember(dest => dest.CollectionRetrievals, opt => opt.Ignore());
            CreateMap<RagCommandRequest, CommandChatRequest>();

            CreateMap<RagChatCommandRequest, CommandChatRequest>()
                .IncludeBase<RagCommandRequest, SimpleCommandRequest>();
            CreateMap<CommandChatRequest, RagChatCommandRequest>()
                .ForMember(dest => dest.QueryCollections, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddingFilters, opt => opt.Ignore())
                .ForMember(dest => dest.CollectionRetrievals, opt => opt.Ignore());

            // LangSearch models
            CreateMap<LangSearchWebSearchDto, WebSearchRequest>()
                .ForMember(dest => dest.Summary, opt => opt.MapFrom(opt => opt.WithSummary));
            CreateMap<LangSearchRankedPageRequestDto, RankedPageRequest>()
                .ForMember(dest => dest.Model, opt => opt.MapFrom(dest => dest.RankingModel));
            CreateMap<LangSearchRankedRequestDto, RankedSearchRequest>()
                .ForMember(dest => dest.QueriedDocuments, opt => opt.MapFrom(dest => dest.Sources));
        }
    }
}
