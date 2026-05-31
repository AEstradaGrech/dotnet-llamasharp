using AutoMapper;
using Dotnet.LangSearch.SDK;
using Dotnet.LangSearch.SDK.Models.Request;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Responses.StructuredOutputs;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Interfaces.Service;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Interfaces.Command.Services;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Request;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
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
    public class PromptingController(IOllamaSharpService ollamaService, IOllamaStreamService streamService, IRagService ragService, IMapper mapper, ILogger<PromptingController> logger) : ControllerBase
    {
        private readonly IOllamaSharpService _ollamaService = ollamaService;
        private readonly IOllamaStreamService _streamService = streamService;
        private readonly IRagService _ragService = ragService;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<PromptingController> _logger = logger;
        
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
            var stream = _streamService.SimplePromptStream(_mapper.Map<ChatPromptRequestDto, Instruction>(request));

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

        [HttpPost("/rag/qa/smart/prompt")]
        public async Task<IActionResult> SimpleSmartPrompt([FromBody] SmartQueryRequestDto request)
        {
            var response = _mapper.Map<RagPrompt, RagPromptResponseDto>(await _ragService.SimpleSmartQuery(_mapper.Map<SmartQueryRequestDto, SmartQueryRequestDEP>(request)));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}
