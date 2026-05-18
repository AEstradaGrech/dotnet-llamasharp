using AutoMapper;
using Dotnet.LangSearch.SDK;
using Dotnet.LangSearch.SDK.Models.Request;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests.Chains;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Services.Inference;
using DotnetLlamaSharp.Domain.Services.Prompting;
using DotnetLlamaSharp.Models.Request;
using DotnetLlamaSharp.Models.Request.Chains;
using DotnetLlamaSharp.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using System.Text.Json.Schema;

namespace DotnetLlamaSharp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromptingController(IOllamaSharpService ollamaService, IOllamaStreamService streamService, IRagService ragService, IPromptCommandsService commandService, 
        ILangSearchService langSearchService, IMapper mapper, ILogger<PromptingController> logger) : ControllerBase
    {
        private readonly IOllamaSharpService _ollamaService = ollamaService;
        private readonly IOllamaStreamService _streamService = streamService;
        private readonly IPromptCommandsService _ollamaCommands = commandService;
        private readonly ILangSearchService _langSearchService = langSearchService;
        private readonly IRagService _ragService = ragService;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<PromptingController> _logger = logger;

        [HttpPost("/langsearch/prompt")]
        public async Task<IActionResult> LangSearchWebData([FromBody] LangSearchWebSearchDto request)
        {
            var response = await _langSearchService.GetWebSearchData(_mapper.Map<LangSearchWebSearchDto, WebSearchRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/langsearch/prompt/pages")]
        public async Task<IActionResult> LangSearchPages([FromBody] LangSearchWebSearchDto request)
        {
            var response = await _langSearchService.GetWebSearchData(_mapper.Map<LangSearchWebSearchDto, WebSearchRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/langsearch/prompt/default/ranked-page")]
        public async Task<IActionResult> LangSearchRankedPageDefault([FromBody] LangSearchWebSearchDto request)
        {
            var response = await _langSearchService.SearchAndRankPages(_mapper.Map<LangSearchWebSearchDto, WebSearchRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/langsearch/prompt/rankedpage")]
        public async Task<IActionResult> LangSearchRankedPage([FromBody] LangSearchRankedPageRequestDto request)
        {
            var response = await _langSearchService.SearchAndRankPages(_mapper.Map<LangSearchRankedPageRequestDto, RankedPageRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/langsearch/ranked/prompt")]
        public async Task<IActionResult> LangSearchReRank([FromBody] LangSearchRankedRequestDto request)
        {
            var data = await _langSearchService.GetWebSearchData(new WebSearchRequest { Count = request.ResultsNumber ?? 1, Query =  request.Query , Summary = false });

            request.Sources = data.WebPage.Results.Select(doc => doc.Snippet).ToList();

            var response = await _langSearchService.GetReRankData(_mapper.Map<LangSearchRankedRequestDto, RankedSearchRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/simple/prompt")]
        public async Task<IActionResult> SimplePrompt([FromBody]SimplePromptRequestDto request)
        {
            var response = await _ollamaService.SimplePrompt(_mapper.Map<SimplePromptRequestDto, SimplePromptRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/simple/prompt/stream")]
        public async Task SimplePromptStream([FromBody] ChatPromptRequestDto request)
        {
            Response.StatusCode = (int)HttpStatusCode.OK;
            Response.ContentType = "text/event-stream";

            // Get the streaming enumerable from the service.
            // returns the _client.GenerateAsync(...) enumerable directly so the controller can iterate the stream.
            var stream = _streamService.SimplePromptStream(_mapper.Map<ChatPromptRequestDto, ChatPromptRequest>(request));

            try
            {
                /*
                    •	sets Content-Type: text/event-stream
                    •	writes bytes to Response.Body and calls FlushAsync
                    •	honors HttpContext.RequestAborted to stop on disconnect

                  Each incoming GenerateResponseStream chunk is emitted as an SSE data: event and flushed immediately so clients get incremental updates.
                */
                await foreach (var chunk in stream.WithCancellation(HttpContext.RequestAborted))
                {
                    if (chunk == null) continue;
                    _logger.LogInformation(chunk.Response);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(chunk.Response ?? string.Empty);
                    await Response.Body.WriteAsync(bytes, 0, bytes.Length, HttpContext.RequestAborted);
                    await Response.Body.FlushAsync(HttpContext.RequestAborted);
                }
            }
            catch (OperationCanceledException)
            {
                // client disconnected
                _logger.LogWarning("-- client disconnected --");
            }
        }

        [HttpPost("/chat/prompt")]
        public async Task<IActionResult> PostChatPrompt([FromBody] ChatPromptRequestDto request)
        {
            var response = _mapper.Map<ChatPrompt, ChatPromptResponseDto>(await _ollamaService.ChatPrompt(_mapper.Map<ChatPromptRequestDto, ChatPromptRequest>(request)));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/guided/chat/prompt")]
        public async Task<IActionResult> GuidedChatPrompt([FromBody] ChatPromptRequestDto request)
        {
            var response = _mapper.Map<ChatPrompt, ChatPromptResponseDto>(await _ollamaService.ChatPrompt(_mapper.Map<ChatPromptRequestDto, ChatPromptRequest>(request), bWithSysmsgUpdate: true));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/rag/qa/y-n/prompt")]
        public async Task<IActionResult> YesNoQuestion([FromBody] RagPromptRequestDto request)
        {
            var response = _mapper.Map<ChatPrompt, ChatPromptResponseDto>(await _ragService.ScoredBinaryQuestion(_mapper.Map<RagPromptRequestDto, RagPromptRequest>(request)));

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
            var response =  await _ollamaCommands.NumericResult(dto.Prompt, dto.SystemMessage, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            return Ok(response);
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
            var response = _mapper.Map<RagPrompt, RagPromptResponseDto>(await _ragService.SimpleSmartQuery(_mapper.Map<SmartQueryRequestDto, SmartQueryRequest>(request)));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/rag/qa/prompt")]
        public async Task<IActionResult> SimpleRagPrompt([FromBody] RagPromptRequestDto request)
        {
            var response = _mapper.Map<RagPrompt, RagPromptResponseDto>(await _ragService.SimpleRagQuery(_mapper.Map<RagPromptRequestDto, RagPromptRequest>(request)));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/rag/chat/prompt")]
        public async Task<IActionResult> RagChatPrompt([FromBody] RagChatRequestDto request)
        {
            var response = _mapper.Map<ChatPrompt, ChatPromptResponseDto>(await _ragService.RagChatPrompt(_mapper.Map<RagChatRequestDto, RagChatRequest>(request)));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/json-schema")]
        public async Task<IActionResult> JsonSchemaFor([FromBody] RagChatRequestDto request)
            => Ok(JsonSerializer.Serialize(JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(RagChatRequestDto))));

        [HttpPost("/chains/test")]
        public async Task<IActionResult> ChaiTests([FromBody] GuidedBatchRequestDto request)
        {
            //var response = _mapper.Map<ChatPrompt, ChatPromptResponseDto>(await _ollamaService.GuidedBatchChainPrompt(_mapper.Map<GuidedBatchRequestDto, GuidedBatchRequest>(request)));

            var response = await _ollamaService.GuidedBatchChain(_mapper.Map<GuidedBatchRequestDto, GuidedBatchRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
        //JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(T))
    }
}
