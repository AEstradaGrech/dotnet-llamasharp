using Dotnet.Chroma.Repositories.Models;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared.Configuration;
using DotnetLlamaSharp.Infrastructure.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using System.Net;

namespace DotnetLlamaSharp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiManagementController(IOllamaApiClient ollama, IOptions<ApiSettings> apiSettings, IOptions<OllamaSettings> ollamaSettings, ILogger<ApiManagementController> logger) : ControllerBase
    {
        private readonly IOllamaApiClient _ollama = ollama;
        private readonly ILogger<ApiManagementController> _logger = logger;
        private readonly ApiSettings _apiSettings = apiSettings.Value;
        private readonly OllamaSettings _ollamaSettings = ollamaSettings.Value;

        [HttpGet("/check")]
        public async Task<IActionResult> GetCheck()
        {
            var healthCheck = $"Timestamp: {DateTime.Now}";
            
            _logger.LogInformation($"-- HEALTHCHECK REQUEST >> {healthCheck} --");
            
            var isServiceUp = await _ollama.IsRunningAsync();
            
            return Ok($"{healthCheck} >> OLLAMA SERVER {(isServiceUp ? "UP" : "DOWN")}");
        }

        [HttpGet("/settings")]
        public async Task<IActionResult> GetSettings()
        {
            if (_apiSettings != null)
                return Ok(_apiSettings);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpGet("/ollama/settings")]
        public async Task<IActionResult> GetOllamaSettings()
        {
            if (_ollamaSettings != null)
                return Ok(_ollamaSettings);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpGet("/embedding/models")]
        public async Task<IActionResult> GetEmebeddingModels()
        {
            if (_apiSettings != null)
                return Ok(_ollamaSettings.EmbeddingModels);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpGet("/models/list")]
        public async Task<IActionResult> GetModels()
            => Ok(await _ollama.ListLocalModelsAsync());

        [HttpGet("/models/pull/{modelName}")]
        public async Task<IActionResult> PullModel(string modelName)
        {
            _logger.LogInformation($"-- Pulling model: {modelName} --");

            PullModelResponse? currentStatus = null;
            
            await foreach (var status in _ollama.PullModelAsync(modelName))
            {
                _logger.LogInformation($"{status.Percent}% {status.Status}");
                if (status == null) continue;

                currentStatus = status;
                if (currentStatus != null)
                    _logger.LogInformation($"{currentStatus.Percent}% {currentStatus.Status}");
            }

            if (currentStatus != null)
                return Ok(currentStatus);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpDelete("/model/{modelName}")]
        public async Task<IActionResult> DeleteModel(string modelName)
        {
            var localModels = await _ollama.ListLocalModelsAsync();

            if (!localModels.Any(x => x.Name == modelName))
                throw new InvalidOperationException($"No local model found with name: {modelName}");

            await _ollama.DeleteModelAsync(modelName);

            return Ok(localModels.SingleOrDefault(x => x.Name == modelName));
        }
    }
}
