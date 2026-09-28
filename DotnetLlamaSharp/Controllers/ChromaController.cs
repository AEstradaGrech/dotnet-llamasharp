using AutoMapper;
using Dotnet.Chroma.Repositories.Models;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Request.Chroma;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Models.Request.Chroma;
using DotnetLlamaSharp.Models.Request.Embeddings;
using DotnetLlamaSharp.Models.Response.Chroma;
using Microsoft.AspNetCore.Mvc;


namespace DotnetLlamaSharp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = nameof(ChromaController))]
    public class ChromaController(IChromaService service, IMapper mapper) : ControllerBase
    {
        private readonly IChromaService _service = service;
        private readonly IMapper _mapper = mapper;

        [HttpGet("/collection/list")]
        public async Task<IActionResult> GetDbCollections()
            => Ok(await _service.GetDbCollections());

        [HttpPost("/new/collection")]
        public async Task<IActionResult> NewCollection([FromBody] CreateCollectionRequestDto dto)
        {
            var request = _mapper.Map<CreateCollectionRequestDto, CreateCollectionRequest>(dto);

            var result = await _service.CreateCollection(request.Name, request.HnswSettings);

            return Ok(result);
        }

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
        
        [HttpPost("/collection/query")]
        public async Task<IActionResult> QueryCollection([FromBody] SimilaritySearchRequestDto request)
            => Ok(_mapper.Map<ChromaQuery, ChromaQueryResponseDto>(await _service.QueryCollection(request.Collection, request.Query.Trim(), request.ResultsNumber, request.MetadataFilters)));

        [HttpGet("/collection/system/{name}/create")]
        public async Task<IActionResult> CreateSysChunksCollection(string name, [FromQuery] string? description)
            => Ok(_mapper.Map<ChromaChunksCollection<ChromaSysChunk>, ChromaChunksCollectionDto<ChromaSysChunkDto>>(await _service.CreateSystemChunksCollection(name, description)));

        [HttpPost("/collection/system/{name}/page")]
        public async Task<IActionResult> GetSystemChunksPage(string name, [FromBody] ChromaPageRequestDto dto)
            => Ok(_mapper.Map<SysChunksCollection, ChromaChunksCollectionDto<ChromaSysChunkDto>>(await _service.GetSysChunksPage(name, dto.PageSize, dto.Page, dto.Filters)));

        [HttpPost("/collection/chats/{name}/page/{page}/size/{pageSize}/chunks")]
        public async Task<IActionResult> GetSessionChunkPage(string name, int page, int pageSize, [FromQuery] bool excludeSession = false)
           => Ok(_mapper.Map<ChromaChatsCollection, ChromaChatsCollectionDto>(await _service.GetChatsCollection(name, excludeSession, null, pageSize, page)));

        [HttpPost("/collection/chats/{name}/{sessionId}/page/{page}/size/{pageSize}/chunks")]
        public async Task<IActionResult> GetSessionChunksPage(string name, string sessionId, int page, int pageSize, [FromQuery] bool excludeSession = false)
           => Ok(_mapper.Map<ChromaChatsCollection, ChromaChatSessionDto>(await _service.GetChatsCollection(name, excludeSession, sessionId, pageSize, page)));

        [HttpPost("/collection/system/{name}/create/message")]
        public async Task<IActionResult> CreateSysChunk(string name, [FromBody] CreateSysChunkRequestDto dto)
            => Ok(_mapper.Map<ChromaSysChunk, ChromaSysChunkDto>(await _service.AddSysMessage(name, _mapper.Map<CreateSysChunkRequestDto,ChromaSysChunk>(dto), dto.Version)));

        [HttpPatch("/collection/system/{collection}/message/{id}")]
        public async Task<IActionResult> PatchSysChunk (string collection, string id, [FromBody] PatchSysChunkRequestDto dto)
            => Ok(_mapper.Map<ChromaSysChunk, ChromaSysChunkDto>(await _service.PatchSysMessage(collection, id, dto.NewText, dto.Metadata)));
        
        [HttpGet("/collection/system/{collection}/message/{name}")]
        public async Task<IActionResult> GetSystemChunk(string collection, string name, [FromQuery] string? version)
            => Ok(_mapper.Map<ChromaSysChunk, ChromaSysChunkDto>(await _service.GetSysMessage(collection, name, version)));

        [HttpDelete("/collection/system/{collection}/message/{name}")]
        public async Task<IActionResult> DeleteSystemChunk(string collection, string name, [FromQuery] string? version)
            => Ok(_mapper.Map<ChromaSysChunk, ChromaSysChunkDto>(await _service.DeleteSysMessage(collection, name, version)));

        [HttpDelete("/collection/system/{collection}/message/id/{id}")]
        public async Task<IActionResult> DeleteSystemChunkById(string collection, string id)
            => Ok(_mapper.Map<ChromaSysChunk, ChromaSysChunkDto>(await _service.DeleteSysMessageById(collection, id)));
    }

}
