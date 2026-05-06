using Microsoft.SemanticKernel.Connectors.Chroma;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Infrastructure.Extensions.Chroma.Models
{
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class DocumentsQueryResultModel : ChromaQueryResultModel
    {
        public List<List<string>> Documents { get; set; } = new List<List<string>>();
    }
}
