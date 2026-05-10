using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Models.Common;
using DotnetLlamaSharp.Models.Request;
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
            
            CreateMap<SimplePromptRequestDto, SimplePromptRequest>();
            CreateMap<SimplePromptRequestDto, SimpleCommandRequest>();

            CreateMap<ChatPromptRequestDto, ChatPromptRequest>()
                .IncludeBase<SimplePromptRequestDto, SimplePromptRequest>();
            CreateMap<RagPromptRequestDto, RagPromptRequest>()
                .IncludeBase<SimplePromptRequestDto, SimplePromptRequest>();
            CreateMap<RagChatRequestDto, RagChatRequest>()
                .IncludeBase<RagPromptRequestDto, RagPromptRequest>();

            // Domain to Domain models
            CreateMap<SimplePromptRequest, RagPromptRequest>()
                .ForMember(dest => dest.CollectionRetrievals, opt => opt.Ignore())
                .ForMember(dest => dest.QueryCollections, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddingFilters, opt => opt.Ignore())
                .ForMember(dest => dest.MinDistance, opt => opt.Ignore());

            CreateMap<PromptSettings, CommandSettings>()
                .ForMember(dest => dest.UseDefaultCommandMessage, opt => opt.Ignore())
                .ForMember(dest => dest.CommandValidations, opt => opt.Ignore())
                .ForMember(dest => dest.ValidationType, opt => opt.Ignore());

            CreateMap<CommandSettings, PromptSettings>();

            CreateMap<SimplePromptRequest, SimpleCommandRequest>().ReverseMap();

            CreateMap<RagPromptRequest, SimplePromptRequest>();
            CreateMap<RagChatRequest, ChatPromptRequest>()
                .IncludeBase<RagPromptRequest, SimplePromptRequest>();
            CreateMap<ChatPromptRequest, RagPromptRequest>()
                .ForMember(dest => dest.QueryCollections, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddingFilters, opt => opt.Ignore())
                .ForMember(dest => dest.CollectionRetrievals, opt => opt.Ignore());
            CreateMap<RagChatRequestDto, RagChatRequest>();

            CreateMap<ScoredStringChoice, ScoredChoiceDto>()
                .ForMember(dest => dest.Choice, opt => opt.MapFrom(src => src.Selected))
                .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => src.Score))
                .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.Justification));
        }
    }
}
