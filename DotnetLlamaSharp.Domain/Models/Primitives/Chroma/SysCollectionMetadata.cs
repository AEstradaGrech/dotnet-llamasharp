using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class SysCollectionMetadata : ChromaCollectionMetadata
    {
        [JsonPropertyName("col_embedding")]
        public ReadOnlyMemory<float> COL_EMBEDDING{ get; set; }
    }
}
