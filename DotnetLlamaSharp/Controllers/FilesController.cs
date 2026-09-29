using AutoMapper;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Interfaces.Service.DocumentLoader;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.DocumentLoader;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Services.DocumentLoader;
using DotnetLlamaSharp.Models.Common.Documents;
using Microsoft.AspNetCore.Mvc;

namespace DotnetLlamaSharp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = nameof(FilesController))]
    public class FilesController(IMapper mapper, IDocumentLoader<PdfLoaderService> pdfLoader, IDocumentLoader<MarkdownLoaderService> markdownLoader) : ControllerBase
    {
        private readonly IMapper _mapper = mapper;
        private readonly IDocumentLoader<PdfLoaderService> _pdfLoader = pdfLoader;
        private readonly IDocumentLoader<MarkdownLoaderService> _markdownLoader = markdownLoader;


        [HttpGet("/pdf/load/{docName}")]
        public async Task<IActionResult> LoadPdfDocument(string docName)
            => Ok(_mapper.Map<Document, DocumentDto>(await _pdfLoader.LoadDocument(docName)));

        [HttpGet("/pdf/load/{docName}/page/{index}")]
        public async Task<IActionResult> LoadPdfDocumentPage(string docName, int index)
            => Ok(_mapper.Map<DocumentPage, DocumentPageDto>(await _pdfLoader.LoadPage(docName, index)));

        [HttpGet("/pdf/load/{docName}/page/{index}/size/{batchSize}")]
        public async Task<IActionResult> LoadPdfDocumentPages(string docName, int index, int batchSize)
            => Ok(_mapper.Map<IEnumerable<DocumentPage>, IEnumerable<DocumentPageDto>>(await _pdfLoader.LoadPages(docName, index, batchSize)));

        [HttpGet("/md/load/{docName}")]
        public async Task<IActionResult> LoadMarkdownDocument(string docName)
            => Ok(_mapper.Map<Document, DocumentDto>(await _markdownLoader.LoadDocument(docName)));

        [HttpGet("/md/load/{docName}/page/{index}")]
        public async Task<IActionResult> LoadMarkdownSection(string docName, int index)
            => Ok(_mapper.Map<DocumentPage, DocumentPageDto>(await _markdownLoader.LoadPage(docName, index)));

        [HttpGet("/md/load/{docName}/page/{index}/size/{batchSize}")]
        public async Task<IActionResult> LoadMarkdownSections(string docName, int index, int batchSize)
            => Ok(_mapper.Map<IEnumerable<DocumentPage>, IEnumerable<DocumentPageDto>>(await _markdownLoader.LoadPages(docName, index, batchSize)));
    }
}
