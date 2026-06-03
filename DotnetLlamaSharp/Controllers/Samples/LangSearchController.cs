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
            var response = await _langSearchService.GetWebSearchData(_mapper.Map<LangSearchWebSearchDto, WebSearchRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/langsearch/prompt/default/ranked-page")]
        public async Task<IActionResult> LangSearchRankedPageDefault([FromBody] LangSearchRankedRequestDto request)
        {
            var response = await _langSearchService.GetReRankData(_mapper.Map<LangSearchRankedRequestDto, RankedSearchRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/langsearch/prompt/rankedpage")]
        public async Task<IActionResult> LangSearchRankedPage([FromBody] LangSearchRankedPageRequestDto request)
        {
            var response = await _langSearchService.SearchAndRankPages(_mapper.Map<LangSearchRankedPageRequestDto, RankedPageRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/langsearch/ranked/prompt")]
        public async Task<IActionResult> LangSearchReRank([FromBody] LangSearchRankedRequestDto request)
        {
            var data = await _langSearchService.GetWebSearchData(new WebSearchRequest { Count = request.ResultsNumber ?? 1, Query = request.Query, Summary = false });

            request.Sources = data.WebPage.Results.Select(doc => doc.Snippet).ToList();

            var response = await _langSearchService.GetReRankData(_mapper.Map<LangSearchRankedRequestDto, RankedSearchRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

    }
}
