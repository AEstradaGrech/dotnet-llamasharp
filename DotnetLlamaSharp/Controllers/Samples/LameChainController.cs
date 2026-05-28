using AutoMapper;
using Dotnet.Chroma.Repositories.Models.Enums;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Responses.StructuredOutputs;
using Dotnet.OllamaSharp.LameChain.SDK.Interfaces.Command.Services;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Request;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Services.Prompting.Samples;
using DotnetLlamaSharp.Models.Request;
using DotnetLlamaSharp.Models.Request.Chains;
using DotnetLlamaSharp.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DotnetLlamaSharp.Controllers.Samples
{
    [Route("api/[controller]")]
    [ApiController]
    public class LameChainController(IMapper mapper, ILameSamplesService samplesService, IPromptCommandsService commandsService) : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ILameSamplesService _samplesService = samplesService;

        // Inject the CommandsService in your app to execute commands or use the IPromptCommandsFactory if you prefer it
        private readonly IPromptCommandsService _ollamaCommands = commandsService;

        [HttpPost("/chains/test")]
        public async Task<IActionResult> ChaiTests([FromBody] ChainedPromptDto request)
        {
            var response = await _samplesService.ParallelChainExample(_mapper.Map<ChainedPromptDto, ChainedPrompt>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/chains/smart-rag")]
        public async Task<IActionResult> SmartRagChain([FromBody] SimplePromptRequestDto request)
        {

            var response = await _samplesService.SmartRagChain(_mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
       
        [HttpPost("/rag/qa/y-n/prompt")]
        public async Task<IActionResult> YesNoQuestion([FromBody] RagPromptRequestDto request)
        {
            var response = _mapper.Map<ChatPrompt, ChatPromptResponseDto>(await _samplesService.ScoredBinaryQuestion(_mapper.Map<RagPromptRequestDto, RagPromptRequest>(request)));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/commands/single-choice/prompt")]
        public async Task<IActionResult> SingleChoice([FromBody] SimplePromptRequestDto dto)
        {
            var request = _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto);

            var response = await _ollamaCommands.StringChoice(dto.Prompt, new List<string> { "FIGHT", "JOIN", "TRADE", "RUNAWAY" }, request.SystemMessage, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/commands/scored-choice/prompt")]
        public async Task<IActionResult> ScoredChoice([FromBody] SimplePromptRequestDto dto)
        {
            var request = _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto);

            var response = _mapper.Map<ScoredStringChoice, ScoredChoiceDto>(
                await _ollamaCommands.ScoredChoice(new List<string> { "FIGHT", "JOIN", "TRADE", "RUNAWAY" }, dto.Prompt, request.SystemMessage, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/commands/boolean/prompt")]
        public async Task<IActionResult> BooleanPrompt([FromBody] SimplePromptRequestDto dto)
        {
            var response = await _ollamaCommands.BooleanChoice(dto.Prompt, dto.SystemMessage, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/commands/scored-boolean/prompt")]
        public async Task<IActionResult> ScoredBooleanPrompt([FromBody] SimplePromptRequestDto dto)
        {
            var response = await _ollamaCommands.ScoredBool(dto.Prompt, dto.SystemMessage, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/commands/yn/prompt")]
        public async Task<IActionResult> StringyBoolPrompt([FromBody] SimplePromptRequestDto dto)
        {
            var response = await _ollamaCommands.StringBoolChoice(dto.Prompt, dto.SystemMessage, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/commands/numeric/prompt")]
        public async Task<IActionResult> NumericPrompt([FromBody] SimplePromptRequestDto dto)
        {
            var response = await _ollamaCommands.NumericResult(dto.Prompt, dto.SystemMessage, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            return response != null ? Ok(response) : Ok("The answer to the query cannot be a number");
        }

        [HttpPost("/commands/enum/prompt")]
        public async Task<IActionResult> EnumPrompt([FromBody] SimplePromptRequestDto dto)
        {
            var response = await _ollamaCommands.EnumChoice<EChunkType>(dto.Prompt, dto.SystemMessage, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            return Ok(response);
        }

        [HttpPost("/rag/qa/smart/prompt")]
        public async Task<IActionResult> SimpleSmartPrompt([FromBody] SmartQueryRequestDto request)
        {
            var response = _mapper.Map<RagPrompt, RagPromptResponseDto>(await _samplesService.SimpleSmartQuery(_mapper.Map<SmartQueryRequestDto, SmartQueryRequestDEP>(request)));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}
