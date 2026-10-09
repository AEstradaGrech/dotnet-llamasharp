using AutoMapper;
using Dotnet.LangSearch.SDK;
using Dotnet.LangSearch.SDK.Models.Request;
using DotnetLlamaSharp.Models.Request.LangSearch;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DotnetLlamaSharp.Controllers.Samples
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = nameof(LangSearchController))]
    public class LangSearchController(IMapper mapper, ILangSearchService langSearchService) : ControllerBase
    {
        private readonly ILangSearchService _langSearchService = langSearchService;
        private readonly IMapper _mapper = mapper;

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
            var response = await _langSearchService.GetWebSearchResults(_mapper.Map<LangSearchWebSearchDto, WebSearchRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/langsearch/prompt/texts")]
        public async Task<IActionResult> LangSearchTexts([FromBody] LangSearchWebSearchDto request)
        {
            var response = await _langSearchService.SearchWebTexts(_mapper.Map<LangSearchWebSearchDto, WebSearchRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}
