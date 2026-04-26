using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Primitives.Embeddings;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Domain.Services.Prompting;
using DotnetLlamaSharp.Models.Request;
using DotnetLlamaSharp.Models.Response;
using Microsoft.AspNetCore.Mvc;
using OllamaSharp.Models;

namespace DotnetLlamaSharp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmbeddingsController(IEmbeddingsService service, IOllamaSharpService ollama, IMapper mapper) : ControllerBase
    {
        private readonly IEmbeddingsService _service = service;
        private readonly IOllamaSharpService _ollama = ollama;
        private readonly IMapper _mapper = mapper;

        [HttpPost("/embeddings/text")]
        public async Task<IActionResult> EmbedText([FromBody] SimpleEmbeddingsRequestDto request)
            => Ok(_mapper.Map<ModelEmbeddings, EmbeddingsResponseDto>(await _service.GenerateEmbeddings(request.Texts.Any() ? request.Texts.FirstOrDefault() : "", request.Dimensions, request.Model)));

        [HttpPost("/embeddings/texts")]
        public async Task<IActionResult> EmbedTexts([FromBody] SimpleEmbeddingsRequestDto request)
            => Ok(_mapper.Map<ModelEmbeddings, EmbeddingsResponseDto>(await _service.GenerateEmbeddings(request.Texts, request.Dimensions, request.Model)));

        [HttpPost("/embeddings/request")]
        public async Task<IActionResult> GetEmbeddings([FromBody] EmbedRequest request)
            => Ok(await _ollama.GetOllamaClientEmbeddings(request));
    }
}
