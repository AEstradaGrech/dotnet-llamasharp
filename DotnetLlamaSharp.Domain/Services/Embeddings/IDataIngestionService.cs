using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.Chroma;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Services.Embeddings
{
    public interface IDataIngestionService
    {
        Task<ChromaCollectionModel> CreateCollectionFromFile(CreateCollectionRequest request);
        
    }
}
