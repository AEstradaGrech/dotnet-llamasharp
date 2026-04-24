using DotnetLlamaSharp.Models.Api.Prompting.Request;
using DotnetLlamaSharp.Services.Inference.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DotnetLlamaSharp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromptingController(IOllamaSharpService ollamaService, ILogger<PromptingController> logger) : ControllerBase
    {
        private readonly IOllamaSharpService _ollamaService = ollamaService;
        private readonly ILogger<PromptingController> _logger = logger;

        [HttpGet("/test-stream")]
        public async Task TestStream()
        {
            Response.StatusCode = (int)HttpStatusCode.OK;
            Response.ContentType = "text/event-stream";
            var stream = _ollamaService.ChatPromptStream(new ChatPromptRequest { Model = "llama3.1:8b", Prompt = "Hi", SystemMessage = "You are Josef K., main character of The Process, by Kafka" });
            await foreach (var chunk in stream.WithCancellation(HttpContext.RequestAborted))
            {
                if (chunk == null) continue;
                _logger.LogInformation(chunk.Message.Content);
                var bytes = System.Text.Encoding.UTF8.GetBytes(chunk.Message.Content ?? string.Empty);
                await Response.Body.WriteAsync(bytes, 0, bytes.Length, HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
            }
        }

        [HttpPost("/simple/prompt")]
        public async Task<IActionResult> SimplePrompt([FromBody]ChatPromptRequest request)
        {
            var response = await _ollamaService.SimplePrompt(request);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/simple/prompt/stream")]
        public async Task SimplePromptStream([FromBody] ChatPromptRequest request)
        {
            Response.StatusCode = (int)HttpStatusCode.OK;
            Response.ContentType = "text/event-stream";

            // Get the streaming enumerable from the service.
            // returns the _client.GenerateAsync(...) enumerable directly so the controller can iterate the stream.
            var stream = _ollamaService.SimplePromptStream(request);

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
        public async Task<IActionResult> ChatPrompt([FromBody] ChatPromptRequest request)
        {
            var response = await _ollamaService.ChatPrompt(request);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/guided/chat/prompt")]
        public async Task<IActionResult> GuidedChatPrompt([FromBody] ChatPromptRequest request)
        {
            var response = await _ollamaService.ChatPrompt(request, bWithSysmsgUpdate: true);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}
