using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Services.Prompting;
using DotnetLlamaSharp.Models.Request;
using DotnetLlamaSharp.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DotnetLlamaSharp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromptingController(IOllamaSharpService ollamaService, IRagService ragService, IMapper mapper, ILogger<PromptingController> logger) : ControllerBase
    {
        private readonly IOllamaSharpService _ollamaService = ollamaService;
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
            var stream = _ollamaService.SimplePromptStream(_mapper.Map<ChatPromptRequestDto, ChatPromptRequest>(request));

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
            var response = _mapper.Map<ChatPrompt, ChatPromptResponseDto>(await _ragService.YesNoQuestion(_mapper.Map<RagPromptRequestDto, RagPromptRequest>(request)));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
        /*
         * 
         * {
  "prompt": "Tell me the last trends in artificial intelligence for videogames",
  "maxTokens": 600,
  "temperature": 0.4,
  "chatHistory": [
   
  ],
  "queryCollections": [
    "rags",
    "cybersecurity"
  ],
  "includedEmbeddings": 4,
  "embeddingFilters": {
    
  }
}
         */

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
    }
}
