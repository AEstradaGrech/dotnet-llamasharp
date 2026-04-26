using DotnetLlamaSharp.Domain.Models.Primitives.Embeddings;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Services.Embeddings
{
    public interface IEmbeddingsService
    {
        Task<ModelEmbeddings> GenerateEmbeddings(string text, int? dimensions = null, string? model = null);
        Task<ModelEmbeddings> GenerateEmbeddings(List<string> texts, int? dimensions = null, string? model = null);
    }
}
