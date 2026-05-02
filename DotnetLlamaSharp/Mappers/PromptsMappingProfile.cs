using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Primitives.Embeddings;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
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
            CreateMap<ChatPromptRequestDto, ChatPromptRequest>();
            CreateMap<RagPromptRequestDto, RagPromptRequest>();

            // Domain to Domain models
            CreateMap<RagPromptRequest, ChatPromptRequest>();
            CreateMap<ChatPromptRequest, RagPromptRequest>()
                .ForMember(dest => dest.QueryCollections, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddingFilters, opt => opt.Ignore())
                .ForMember(dest => dest.CollectionRetrievals, opt => opt.Ignore());
            CreateMap<RagChatRequestDto, RagChatRequest>();
        }
    }
}
