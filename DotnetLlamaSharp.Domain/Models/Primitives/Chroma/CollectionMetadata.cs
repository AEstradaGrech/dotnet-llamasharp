using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class CollectionMetadata : ChromaMetadata
    {
        [JsonPropertyName("files")]
        public string FILES { get; set; } = "";

        [JsonPropertyName("chunks")]
        public int CHUNKS { get; set; }

        [JsonPropertyName("chunk_sizes")]
        public string CHUNK_SIZES { get; set; } = "";
        [JsonPropertyName("chunk_overlaps")]
        public string CHUNK_OVERLAPS { get; set; } = "";

        [JsonPropertyName("skipped_pages")]
        public string SKIPPED_PAGES { get; set; } = "";
        [JsonPropertyName("page_cutoffs")]
        public string PAGE_CUTOFFS { get; set; } = "";

    }
}
