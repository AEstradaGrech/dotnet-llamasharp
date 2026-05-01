using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Repositories.Chroma
{
    public interface IChromaChunksRepository : IChromaRepository<ChromaChunksCollection<ChromaChunk>, ChromaChunk>
    {
    }
}
