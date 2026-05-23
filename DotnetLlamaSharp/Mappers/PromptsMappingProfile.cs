using AutoMapper;
using Dotnet.LangSearch.SDK.Models.Request;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Responses.StructuredOutput;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Request;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Models.Common;
using DotnetLlamaSharp.Models.Request;
using DotnetLlamaSharp.Models.Request.Chains;
using DotnetLlamaSharp.Models.Response;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Mappers
{
    public class PromptsMappingProfile : Profile
    {
        public PromptsMappingProfile()
        {
            //Ollama Model to Domain Object
            CreateMap<Message, ChatMessage>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
            
            //Domain Object to OllamaModels
            CreateMap<ChatMessage, Message>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => new ChatRole(src.Role)));

            //Domain Object to DTO
            CreateMap<ChatMessage, ChatMessageDto>()
                .ReverseMap();

            CreateMap<ChatPrompt, ChatPromptResponseDto>();
            CreateMap<RagPrompt, RagPromptResponseDto>()
                .IncludeBase<ChatPrompt, ChatPromptResponseDto>();

            //DTO to Domain Object
            CreateMap<PromptSettingsDto, PromptSettings>();
            CreateMap<PromptSettingsDto, CommandSettings>();

            CreateMap<PromptSettings, PromptSettingsDto>()
                .ForMember(dest => dest.UseDefaultCommandMessage, opt => opt.Ignore())
                .ForMember(dest => dest.CommandValidations, opt => opt.Ignore())
                .ForMember(dest => dest.ValidationType, opt => opt.Ignore());

            CreateMap<CommandSettings, PromptSettingsDto>();
            
            CreateMap<SimplePromptRequestDto, SimplePromptRequest>()
                .ForMember(dest => dest.Settings, opt => opt.MapFrom(src => src.Settings));
            CreateMap<SimplePromptRequestDto, SimpleCommandRequest>()
                .ForMember(dest => dest.Settings, opt => opt.MapFrom(src => src.Settings));

            CreateMap<ChatPromptRequestDto, ChatPromptRequest>()
                .IncludeBase<SimplePromptRequestDto, SimplePromptRequest>();
            CreateMap<RagPromptRequestDto, RagPromptRequest>()
                .IncludeBase<SimplePromptRequestDto, SimplePromptRequest>();
            CreateMap<RagChatRequestDto, RagChatRequest>()
                .IncludeBase<RagPromptRequestDto, RagPromptRequest>();
            CreateMap<SmartQueryRequestDto, SmartQueryRequest>()
                .IncludeBase<SimplePromptRequestDto, SimpleCommandRequest>();
            // Domain to Domain models

            CreateMap<PromptSettings, CommandSettings>()
               .ForMember(dest => dest.UseDefaultCommandMessage, opt => opt.Ignore())
               .ForMember(dest => dest.CommandValidations, opt => opt.Ignore())
               .ForMember(dest => dest.ValidationType, opt => opt.Ignore());
            CreateMap<CommandSettings, PromptSettings>();
            CreateMap<CommandSettings, CommandSettings>();

            CreateMap<SimplePromptRequest, SimpleCommandRequest>().ReverseMap();

            CreateMap<SimplePromptRequest, RagPromptRequest>()
                .ForMember(dest => dest.CollectionRetrievals, opt => opt.Ignore())
                .ForMember(dest => dest.QueryCollections, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddingFilters, opt => opt.Ignore())
                .ForMember(dest => dest.MinDistance, opt => opt.Ignore());
            CreateMap<SimpleCommandRequest, RagPromptRequest>()
               .ForMember(dest => dest.CollectionRetrievals, opt => opt.Ignore())
                .ForMember(dest => dest.QueryCollections, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddingFilters, opt => opt.Ignore())
                .ForMember(dest => dest.MinDistance, opt => opt.Ignore());

            CreateMap<SmartQueryRequest, SimplePromptRequest>()
                .ForMember(dest => dest.Settings, opt => opt.MapFrom(opt => opt.Settings));
            CreateMap<SmartQueryRequest, SimpleCommandRequest>()
                .ForMember(dest => dest.Settings, opt => opt.MapFrom(opt => opt.Settings));

            CreateMap<SimplePromptRequest, RagPromptRequest>()
                .ForMember(dest => dest.CollectionRetrievals, opt => opt.Ignore())
                .ForMember(dest => dest.QueryCollections, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddingFilters, opt => opt.Ignore())
                .ForMember(dest => dest.MinDistance, opt => opt.Ignore());
            CreateMap<SimpleCommandRequest, RagPromptRequest>()
                .ForMember(dest => dest.CollectionRetrievals, opt => opt.Ignore())
                .ForMember(dest => dest.QueryCollections, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddingFilters, opt => opt.Ignore())
                .ForMember(dest => dest.MinDistance, opt => opt.Ignore());

            CreateMap<SmartQueryRequest, SmartRagSettings>()
                .ForMember(dest => dest.MaxExamples, opt => opt.MapFrom(src => src.MaxFewShotExamples))
                .ForMember(dest => dest.WithQueryAugmentation, opt => opt.Ignore())
                .ForMember(dest => dest.WithRagExpansion, opt => opt.Ignore());

            CreateMap<RagPromptRequest, SimplePromptRequest>();
            CreateMap<RagPromptRequest, SimpleCommandRequest>();

            CreateMap<RagChatRequest, ChatPromptRequest>()
                .IncludeBase<RagPromptRequest, SimplePromptRequest>();
            CreateMap<RagChatRequest, ChatPromptRequest>();

            CreateMap<ChatPromptRequest, RagPromptRequest>()
                .ForMember(dest => dest.QueryCollections, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddingFilters, opt => opt.Ignore())
                .ForMember(dest => dest.CollectionRetrievals, opt => opt.Ignore());

            CreateMap<RagChatRequestDto, RagChatRequest>();
            CreateMap<ScoredStringChoice, ScoredChoiceDto>()
                .ForMember(dest => dest.Choice, opt => opt.MapFrom(src => src.Selected))
                .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => src.Score))
                .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.Justification));

            CreateMap<LangSearchWebSearchDto, WebSearchRequest>()
                .ForMember(dest => dest.Summary, opt => opt.MapFrom(opt => opt.WithSummary));
            CreateMap<LangSearchRankedPageRequestDto, RankedPageRequest>()
                .ForMember(dest => dest.Model, opt => opt.MapFrom(dest => dest.RankingModel));
            CreateMap<LangSearchRankedRequestDto, RankedSearchRequest>()
                .ForMember(dest => dest.QueriedDocuments, opt => opt.MapFrom(dest => dest.Sources));

            CreateMap<InstructionDto, Instruction>().ReverseMap();

            CreateMap<ChainedPromptDto, ChainedPrompt>();
        }
    }
}
