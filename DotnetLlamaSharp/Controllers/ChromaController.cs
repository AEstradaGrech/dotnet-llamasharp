using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
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
            => Ok(_mapper.Map<ChromaChunksCollection<ChromaChunk>, ChromaChunksCollectionDto<ChromaChunkDto>>(await _service.GetCollection(name)));

        [HttpDelete("/collection/{name}")]
        public async Task<IActionResult> DeleteCollection(string name)
            => Ok(await _service.DeleteCollection(name));

        [HttpPost("/collection/create")]
        public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequestDto request)
            => Ok(_mapper.Map<ChromaFilesCollection, ChromaFilesCollectionDto>(await _service.CreateEmptyFileCollection(_mapper.Map<CreateCollectionRequestDto, CreateCollectionRequest>(request))));

        [HttpPost("/collection/embed")]
        public async Task<IActionResult> EmbedCollection([FromBody]EmbedCollectionRequestDto request)
            => Ok(_mapper.Map<ChromaFilesCollection, ChromaFilesCollectionDto>(await _service.CreateCollectionFromFile(_mapper.Map<EmbedCollectionRequestDto, EmbedCollectionRequest>(request))));

        [HttpPost("/collection/inspect/file")]
        public async Task<IActionResult> InspectFilesCollection([FromBody] InspectCollectionRequestDto request) 
            => Ok(_mapper.Map<ChromaFilesCollection, ChromaFilesCollectionDto>(await _service.InspectFilesCollection(request.Name, request.StartIndex, request.SamplesNumber, request.IncludeEmbeddings)));

        [HttpGet("/collection/{name}/inspect/chunk/{id}")]
        public async Task<IActionResult> InspectChunk(string name, string id)
            => Ok(_mapper.Map<ChromaChunk, ChromaChunkDto>(await _service.InspectChunk(name, id)));
        [HttpPost("/collection/chats/{name}")]
        public async Task<IActionResult> GetChatsCollection(string name)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/collection/query")]
        public async Task<IActionResult> QueryCollection([FromBody] SimilaritySearchRequestDto request)
            => Ok(_mapper.Map<ChromaQuery, ChromaQueryResponseDto>(await _service.QueryCollection(request.Collection, request.Query.Trim(), request.ResultsNumber, request.MetadataFilters)));

        [HttpGet("/collection/system/{name}/create")]
        public async Task<IActionResult> CreateSysChunksCollection(string name, [FromQuery] string? description)
            => Ok(_mapper.Map<ChromaChunksCollection<ChromaSysChunk>, ChromaChunksCollectionDto<ChromaSysChunkDto>>(await _service.CreateSystemChunksCollection(name, description)));

        [HttpPost("/collection/system/{name}/create/message")]
        public async Task<IActionResult> CreateSysChunksCollection(string name, [FromBody] CreateSysChunkRequestDto dto)
            => Ok(_mapper.Map<ChromaSysChunk, ChromaSysChunkDto>(await _service.AddSysMessage(name, _mapper.Map<CreateSysChunkRequestDto,ChromaSysChunk>(dto), dto.Version)));

        [HttpGet("/collection/system/{collection}/message/{name}")]
        public async Task<IActionResult> GetSystemChunk(string collection, string name, [FromQuery] string? version)
            => Ok(_mapper.Map<ChromaSysChunk, ChromaSysChunkDto>(await _service.GetSysMessage(collection, name, version)));

        [HttpDelete("/collection/system/{collection}/delete/{name}")]
        public async Task<IActionResult> DeleteSystemChunk(string collection, string name, [FromQuery] string? version)
            => Ok(_mapper.Map<ChromaSysChunk, ChromaSysChunkDto>(await _service.DeleteSysMessage(collection, name, version)));

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
