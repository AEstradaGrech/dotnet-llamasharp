using Microsoft.AspNetCore.Mvc;

namespace DotnetLlamaSharp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthCheckController : ControllerBase
    {

        [HttpGet("/check")]
        public async Task<IActionResult> GetCheck()
        {
            return Ok($"Timestamp: {DateTime.Now}");
        }
    }
}
