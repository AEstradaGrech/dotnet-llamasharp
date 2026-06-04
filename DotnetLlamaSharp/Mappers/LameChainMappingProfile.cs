using AutoMapper;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Responses.StructuredOutputs;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Request;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Models.Request.Prompting;
using DotnetLlamaSharp.Models.Request;
using DotnetLlamaSharp.Models.Request.Chains;
using DotnetLlamaSharp.Models.Response;
using OllamaSharp.Models.Chat;

namespace DotnetLlamaSharp.Mappers
{
    public class LameChainMappingProfile : Profile
    {
        public LameChainMappingProfile()
        {

            //Ollama Model to Lame Chain Object
            CreateMap<Message, ChatMessage>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

            //Lame Chain Message Object to Ollama Models
            CreateMap<ChatMessage, Message>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => new ChatRole(src.Role)));

            CreateMap<ScoredStringChoice, ScoredChoiceDto>()
                .ForMember(dest => dest.Choice, opt => opt.MapFrom(src => src.Selected))
                .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => src.Score))
                .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.Justification));

            CreateMap<InstructionDto, Instruction>().ReverseMap();
            CreateMap<ChainedPromptDto, ChainedPrompt>();
        }
    }
}
