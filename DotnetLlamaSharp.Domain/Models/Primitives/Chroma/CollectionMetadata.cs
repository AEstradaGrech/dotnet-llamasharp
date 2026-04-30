using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Chroma
{
    public class CollectionMetadata : ChromaMetadata
    {
        [JsonPropertyName("files")]
        public string FILES { get; set; }

        [JsonPropertyName("chunks")]
        public int CHUNKS { get; set; }

        [JsonPropertyName("chunk_size")]
        public int CHUNK_SIZE { get; set; }
        [JsonPropertyName("chunk_overlap")]
        public int CHUNK_OVERLAP { get; set; }

        [JsonPropertyName("skipped_pages")]
        public int SKIPPED_PAGES { get; set; }
        [JsonPropertyName("page_cutoff")]
        public int PAGE_CUTOFF { get; set; }

    }
}
