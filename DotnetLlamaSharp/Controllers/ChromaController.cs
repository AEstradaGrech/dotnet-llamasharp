using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.DocumentLoader;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.DocumentLoader;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Infrastructure.Services.DocumentLoaders;
using DotnetLlamaSharp.Models.Common.Documents;
using DotnetLlamaSharp.Models.Request;
using DotnetLlamaSharp.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DotnetLlamaSharp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChromaController(IChromaService service, IDocumentLoader<PdfLoaderService> docLoader, IMapper mapper, IDocumentLoader<WordLoaderService> wordLoader) : ControllerBase
    {
        private readonly IChromaService _service = service;
        private readonly IDocumentLoader<WordLoaderService> _wordLoader = wordLoader;
        private readonly IDocumentLoader<PdfLoaderService> _loader = docLoader;
        private readonly IMapper _mapper = mapper;
        
        [HttpGet("/collection/list")]
        public async Task<IActionResult> GetDbCollections()
            => Ok(await _service.GetDbCollections());

        [HttpGet("/collection/{name}")]
        public async Task<IActionResult> GetCollection(string name)
            => Ok(_mapper.Map<ChromaCollection, ChromaCollectionDto>(await _service.GetCollection(name)));

        [HttpDelete("/collection/{name}")]
        public async Task<IActionResult> DeleteCollection(string name)
            => Ok(await _service.DeleteCollection(name));

        [HttpPost("/collection/create")]
        public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequestDto request)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/collection/embed")]
        public async Task<IActionResult> EmbedCollection([FromBody]EmbedCollectionRequestDto request)
            => Ok(_mapper.Map<ChromaCollection, ChromaCollectionDto>(await _service.CreateCollectionFromFile(_mapper.Map<EmbedCollectionRequestDto, EmbedCollectionRequest>(request))));

        [HttpGet("/documents/load/{docName}")]
        public async Task<IActionResult> LoadDocument(string docName)
            => Ok(_mapper.Map<Document, DocumentDto>(await _loader.LoadDocument(docName)));

        [HttpGet("/documents/load/{docName}/page/{index}")]
        public async Task<IActionResult> LoadDocumentPage(string docName, int index)
            => Ok(_mapper.Map<DocumentPage, DocumentPageDto>(await _loader.LoadPage(docName, index)));

        [HttpGet("/documents/load/{docName}/page/{index}/size/{batchSize}")]
        public async Task<IActionResult> LoadDocumentPages(string docName, int index, int batchSize)
            => Ok(_mapper.Map<IEnumerable<DocumentPage>, IEnumerable<DocumentPageDto>>(await _loader.LoadPages(docName, index, batchSize)));

        [HttpGet("/documents/load/word/{docName}")]
        public async Task<IActionResult> LoadWordDocument(string docName)
            => Ok(_mapper.Map<Document, DocumentDto>(await _wordLoader.LoadDocument(docName)));
    }

}
