using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Infrastructure.Extensions.Chroma.Models
{
    public class ChromaClientError
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
        public HttpStatusCode Code { get; set; } = HttpStatusCode.BadRequest;
    }
}
