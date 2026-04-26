using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Primitives.DocumentLoader;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.DocumentLoader;
using DotnetLlamaSharp.Infrastructure.Services.DocumentLoaders;
using DotnetLlamaSharp.Models.Common.Documents;
using Microsoft.AspNetCore.Mvc;

namespace DotnetLlamaSharp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChromaController(IChromaRepository repo, IDocumentLoader<PdfLoaderService> docLoader, IMapper mapper, IDocumentLoader<WordLoaderService> wordLoader) : ControllerBase
    {
        private readonly IChromaRepository _repo = repo;
        private readonly IDocumentLoader<WordLoaderService> _wordLoader = wordLoader;
        private readonly IDocumentLoader<PdfLoaderService> _loader = docLoader;
        private readonly IMapper _mapper = mapper;

        [HttpGet("/collection/list")]
        public async Task<IActionResult> GetDbCollections()
            => Ok(await _repo.GetDbCollections());

        [HttpGet("/collection/create/{collectionName}")]
        public async Task<IActionResult> CreateCollection(string collectionName)
            => Ok(await _repo.CreateCollection(collectionName));

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
