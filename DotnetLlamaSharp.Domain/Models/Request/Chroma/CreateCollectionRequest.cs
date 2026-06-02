using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Request.Chroma
{
    public class CreateCollectionRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? EmbeddingModel { get; set; }
        public int Dimensions { get; set; }
    }
}
